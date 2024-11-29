using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using StackExchange.Redis;

namespace PayoUrl.Creator.Services;

public class IncrementalIdGenerator(IConnectionMultiplexer redisConnection, ILogger<IncrementalIdGenerator> logger)
{
    public class IncrementalIdGenerationException : Exception;
    private const string Key = "shorturl-id";

    public async Task<long> GetNewIdAsync()
    {
        var db = redisConnection.GetDatabase();

        var retryPolicy = GetRetryPolicy(db);

        try
        {
            return await retryPolicy.ExecuteAsync(async () => await db.StringIncrementAsync(Key));
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to generate a new incremental ID.");
            throw new IncrementalIdGenerationException();
        }
    }

    private AsyncRetryPolicy GetRetryPolicy(IDatabase db)
    {
        const int maxRetryCount = 1;
        const string lockKey = $"{Key}_lock";
        var lockValue = Guid.NewGuid().ToString(); // Unique value to identify the lock owner
        var lockExpiry = TimeSpan.FromSeconds(1); // Set a reasonable expiry for the lock

        var retryPolicy = Policy
            .Handle<RedisServerException>(ex =>
                ex.Message.Contains("WRONGTYPE") ||
                ex.Message.Contains("ERR value is not an integer or out of range"))
            .RetryAsync(maxRetryCount, async (exception, retryCount, _) =>
            {
                if (exception is RedisServerException redisEx)
                {
                    logger.LogWarning($"Retry {retryCount} due to {exception.Message}");

                    switch (redisEx.Message)
                    {
                        case var msg when msg.Contains("WRONGTYPE"):
                            logger.LogError("Error: The key '{key}' contains a non-integer or incompatible type.", Key);
                            if (await db.LockTakeAsync(lockKey, lockValue, lockExpiry))
                            {
                                try
                                {
                                    if (await db.KeyDeleteAsync(Key))
                                    {
                                        logger.LogInformation("Index cache key was deleted and can be re-created.");
                                    }
                                }
                                finally
                                {
                                    await db.LockReleaseAsync(lockKey, lockValue);
                                }
                            }
                            break;
                        case var msg when msg.Contains("ERR value is not an integer or out of range"):
                            logger.LogError("Error: The value at Key '{key}' cannot be incremented because it's out of range.", Key);
                            if (await db.LockTakeAsync(lockKey, lockValue, lockExpiry))
                            {
                                try
                                {
                                    if (await db.StringSetAsync(Key, 0))
                                    {
                                        logger.LogInformation("Index cache key was returned to zero."); 
                                    }
                                }
                                finally
                                {
                                    await db.LockReleaseAsync(lockKey, lockValue);
                                }
                            }
                            break;
                    }
                }
            });

        return retryPolicy;
    }
}
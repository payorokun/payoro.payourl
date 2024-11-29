using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using StackExchange.Redis;

namespace PayoUrl.Creator.Services;

public class IncrementalIdGenerator(IConnectionMultiplexer redisConnection, ILogger<IncrementalIdGenerator> logger)
{
    public class IncrementalIdGenerationException : Exception;

    public async Task<long> GetNewIdAsync()
    {
        const string key = "shorturl-id";
        const string lockKey = $"{key}_lock";
        var lockValue = Guid.NewGuid().ToString(); // Unique value to identify the lock owner
        var lockExpiry = TimeSpan.FromSeconds(1); // Set a reasonable expiry for the lock

        var db = redisConnection.GetDatabase();

        var retryPolicy = GetRetryPolicy(key, db, lockKey, lockValue, lockExpiry);

        try
        {
            return await retryPolicy.ExecuteAsync(async () => await db.StringIncrementAsync(key));
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to generate a new incremental ID.");
            throw new IncrementalIdGenerationException();
        }
    }

    private AsyncRetryPolicy GetRetryPolicy(string key, IDatabase db, string lockKey, string lockValue, TimeSpan lockExpiry)
    {
        const int maxRetryCount = 1;

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
                            logger.LogError("Error: The key '{key}' contains a non-integer or incompatible type.", key);
                            if (await db.LockTakeAsync(lockKey, lockValue, lockExpiry))
                            {
                                try
                                {
                                    if (await db.KeyDeleteAsync(key))
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
                            logger.LogError("Error: The value at Key '{key}' cannot be incremented because it's out of range.", key);
                            if (await db.LockTakeAsync(lockKey, lockValue, lockExpiry))
                            {
                                try
                                {
                                    if (await db.StringSetAsync(key, 0))
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
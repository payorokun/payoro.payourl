using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace PayoUrl.Creator;

public class RedisIncrementalIdGenerator(IConnectionMultiplexer redisConnection, ILogger<RedisIncrementalIdGenerator> logger)
{
    public class RetrievalException : Exception;

    private const string Key = "shorturl-id";
    public async Task<long> IncrementReadId()
    {
        const string lockKey = $"{Key}_lock";
        var lockValue = Guid.NewGuid().ToString(); // Unique value to identify the lock owner
        var lockExpiry = TimeSpan.FromSeconds(1); // Set a reasonable expiry for the lock

        var db = redisConnection.GetDatabase();
        const int maxRetryCount = 1;
        var retryCount = 0;

        do
        {
            try
            {
                var result = await db.StringIncrementAsync(Key);
                return result;
            }
            catch (RedisServerException ex) when (ex.Message.Contains("WRONGTYPE"))
            {
                // Handle the case where the key is of the wrong type
                logger.LogError("Error: The key '{0}' contains a non-integer or incompatible type.", Key);
                
                // Attempt to acquire the lock
                if (await db.LockTakeAsync(lockKey, lockValue, lockExpiry))
                {
                    try
                    {
                        // Correct the error by deleting the key, then release the lock
                        await db.KeyDeleteAsync(Key);
                    }
                    finally
                    {
                        // Release the lock after correction
                        await db.LockReleaseAsync(lockKey, lockValue);
                    }
                }
            }
            catch (RedisServerException ex) when (ex.Message.Contains("ERR value is not an integer or out of range"))
            {
                // Handle the case where the value cannot be represented as an integer
                logger.LogError("Error: The value at '{0}' cannot be incremented because it's not an integer.", Key);
                
                // Attempt to acquire the lock
                if (await db.LockTakeAsync(lockKey, lockValue, lockExpiry))
                {
                    try
                    {
                        // Correct the error by setting the key to zero, then release the lock
                        await db.StringSetAsync(Key, 0);
                    }
                    finally
                    {
                        // Release the lock after correction
                        await db.LockReleaseAsync(lockKey, lockValue);
                    }
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, e.Message);
                throw new RetrievalException();
            }
            // After deleting the key OR updating the value, attempt the increment operation again
            retryCount++;
        } while (retryCount < maxRetryCount);

        throw new RetrievalException();
    }
}
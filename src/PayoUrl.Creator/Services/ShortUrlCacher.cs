using Microsoft.Extensions.Logging;
using PayoUrl.Creator.Models;
using StackExchange.Redis;

namespace PayoUrl.Creator.Services;

public class ShortUrlCache(IConnectionMultiplexer redisConnection, ILogger<ShortUrlCache> logger)
{
    public async Task StoreAsync(ShortenedUrlEntity entity)
    {
        var db = redisConnection.GetDatabase();

        try
        {
            await db.StringSetAsync(entity.ShortCode, entity.LongUrl);
        }
        catch (RedisServerException ex)
        {
            logger.LogError(ex, "Error: Failed to cache the new item with key '{0}'.", entity.ShortCode);
        }
    }
}
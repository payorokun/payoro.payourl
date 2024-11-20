using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using PayoUrl.Creator.Models;

namespace PayoUrl.Creator;

public class ShortUrlGenerator(
    ILogger<ShortUrlGenerator> logger,
    RedisIncrementalIdGenerator redisIncrementalIdGenerator,
    Container payoUrlContainer,
    NumberToShortStringEncoder encoder)
{
    public class GenerationException(string message, Exception innerException = null)
        : Exception(message, innerException);

    public async Task<UrlEntry> CreateAsync(string longUrl)
    {
        long newId;
        try
        {
            newId = await redisIncrementalIdGenerator.IncrementReadId();
        }
        catch (RedisIncrementalIdGenerator.RetrievalException ex)
        {
            throw new GenerationException("Failed to generate a valid ID.", ex);
        }

        string encodedId;
        try
        {
            encodedId = encoder.Encode(newId);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            throw new GenerationException(ex.Message, ex);
        }

        UrlEntry newItem;
        try
        {
            var createdItem = await payoUrlContainer.CreateItemAsync(new UrlEntry
            {
                Id = encodedId,
                LongUrl = longUrl,
                CreatedAt = DateTime.UtcNow
            }, partitionKey: new PartitionKey(encodedId));
            newItem = createdItem.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            logger.LogError("A conflict occurred while trying to create the item. ID: {Id}, Long URL: {LongUrl}", encodedId, longUrl);
            throw new GenerationException("A conflict occurred while trying to create the URL entry.", ex);
        }

        return newItem;
    }
}
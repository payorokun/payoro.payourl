using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using PayoUrl.Creator.Models;
using Polly;

namespace PayoUrl.Creator.Services;

public class ShortUrlGenerator(
    ILogger<ShortUrlGenerator> logger,
    IncrementalIdGenerator incrementalIdGenerator,
    Container payoUrlDatabase,
    ShortUrlCache shortUrlCache,
    NumberToShortStringEncoder encoder)
{
    public class GenerationException(string message, Exception innerException = null)
        : Exception(message, innerException);
    public class ConflictException(string shortId, string longUrl, Exception innerException = null)
        : Exception($"A conflict occurred while trying to create the item. ID: {shortId}, Long URL: {longUrl}", innerException);


    public async Task<UrlEntry> CreateAsync(string longUrl)
    {
        var retryPolicy = Policy
            .Handle<ConflictException>()
            .RetryAsync(10, (ex, retryCount) =>
            {
                logger.LogError(ex, ex.Message);
                logger.LogWarning("Retrying due to conflict. Attempt {RetryCount}", retryCount);
            });

        var fallbackPolicy = Policy<UrlEntry>
            .Handle<ConflictException>()
            .FallbackAsync(
                fallbackValue: null,
                onFallbackAsync: (exception, _) =>
                {
                    logger.LogError(exception.Exception, "All retry attempts failed to resolve the cosmos creation conflict for URL: {LongUrl}", longUrl);
                    return Task.CompletedTask;
                });
        try
        {
            ItemResponse<UrlEntry> newItem = null;

            await fallbackPolicy.WrapAsync(retryPolicy).ExecuteAsync(async () =>
            {
                newItem = await GetValue(longUrl);
                if (newItem.StatusCode == HttpStatusCode.Created)
                {
                    logger.LogInformation("Item with ID {ID} for LongUrl {LongUrl} created in database", newItem.Resource.Id, newItem.Resource.LongUrl);
                }
                return newItem.Resource;
            });
            return newItem;

        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
            throw new GenerationException(ex.Message, ex);
        }
    }

    private async Task<ItemResponse<UrlEntry>> GetValue(string longUrl)
    {
        var newId = await incrementalIdGenerator.GetNewIdAsync();

        var encodedId = encoder.Encode(newId);

        ItemResponse<UrlEntry> createdItem;

        try
        {
            createdItem = await payoUrlDatabase.CreateItemAsync(new UrlEntry
            {
                Id = encodedId,
                LongUrl = longUrl,
                CreatedAt = DateTime.UtcNow
            }, partitionKey: new PartitionKey(encodedId));
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
        {
            throw new ConflictException(encodedId, longUrl, ex);
        }

        await shortUrlCache.StoreAsync(createdItem.Resource);

        return createdItem;

    }
}
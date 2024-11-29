using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using PayoUrl.Creator.Models;
using Polly;
using Polly.Wrap;

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
    public class ConflictException(ShortenedUrlEntity entity, Exception innerException = null)
        : Exception($"A conflict occurred while trying to create item with Short Code: {entity.ShortCode}, Long URL: {entity.LongUrl}", innerException);


    public async Task<ShortenedUrlEntity> GenerateAsync(string longUrl)
    {
        var policy = GetRetryPolicyFor(longUrl);

        try
        {
            var newItem = await policy.ExecuteAsync(async () => await RegisterShortenedUrl(longUrl));

            await shortUrlCache.StoreAsync(newItem);

            return newItem;

        }
        catch (Exception ex)
        {
            logger.LogError(ex, ex.Message);
            throw new GenerationException(ex.Message, ex);
        }
    }

    private async Task<ShortenedUrlEntity> RegisterShortenedUrl(string longUrl)
    {
        var entity = await GetNewShortenedUrlEntity(longUrl);
        ItemResponse<ShortenedUrlEntity> databaseGeneratedItem;

        try
        {
            databaseGeneratedItem = await payoUrlDatabase.CreateItemAsync(entity, partitionKey: new PartitionKey(entity.ShortCode));
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
        {
            throw new ConflictException(entity, ex);
        }

        if (databaseGeneratedItem.StatusCode != HttpStatusCode.Created)
        {
            throw new GenerationException($"Failed to create database item for {longUrl}. Status Code: {databaseGeneratedItem.StatusCode}");
        }

        logger.LogInformation("Item with Short Code {ShortCode} for LongUrl {LongUrl} created in database", databaseGeneratedItem.Resource.ShortCode, databaseGeneratedItem.Resource.LongUrl);

        return databaseGeneratedItem.Resource;
    }

    private async Task<ShortenedUrlEntity> GetNewShortenedUrlEntity(string longUrl)
    {
        var newId = await incrementalIdGenerator.GetNewIdAsync();
        var shortCode = encoder.Encode(newId);
        return new ShortenedUrlEntity
        {
            ShortCode = shortCode,
            LongUrl = longUrl,
            CreatedAt = DateTime.UtcNow
        };
    }

    private AsyncPolicyWrap<ShortenedUrlEntity> GetRetryPolicyFor(string longUrl)
    {
        var retryPolicy = Policy
            .Handle<ConflictException>()
            .RetryAsync(10, (ex, retryCount) =>
            {
                logger.LogError(ex, ex.Message);
                logger.LogWarning("Retrying due to conflict. Attempt {RetryCount}", retryCount);
            });

        var fallbackPolicy = Policy<ShortenedUrlEntity>
            .Handle<ConflictException>()
            .FallbackAsync(
                fallbackValue: null,
                onFallbackAsync: (exception, _) =>
                {
                    logger.LogError(exception.Exception, "All retry attempts failed to resolve the database conflict for URL: {LongUrl}", longUrl);
                    return Task.CompletedTask;
                });
        var policy = fallbackPolicy.WrapAsync(retryPolicy);
        return policy;
    }
}
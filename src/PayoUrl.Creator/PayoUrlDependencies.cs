using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using PayoUrl.Creator.Services;
using StackExchange.Redis;

namespace PayoUrl.Creator;
public static class PayoUrlDependencies
{
    public static void AddDependencies(this IServiceCollection services)
    {
        services.AddRedis();
        services.AddCosmos();
        services.AddServices();
    }
    
    private static void AddRedis(this IServiceCollection services)
    {
        var redisConnectionString = Environment.GetEnvironmentVariable("RedisConnectionString");
        if (string.IsNullOrEmpty(redisConnectionString))
        {
            throw new InvalidOperationException("The Redis connection string is not set in the environment variables.");
        }

        var connectionMultiplexer = ConnectionMultiplexer.Connect(redisConnectionString);
        services.AddSingleton<IConnectionMultiplexer>(connectionMultiplexer);
    }

    private static void AddCosmos(this IServiceCollection services)
    {
        const string databaseId = "payoro.dev";
        const string containerId = "payourl";

        var cosmosConnectionString = Environment.GetEnvironmentVariable("CosmosConnectionString");
        var client = new CosmosClient(cosmosConnectionString);

        var container = client.GetContainer(databaseId, containerId);
        services.AddSingleton(container);
    }

    private static void AddServices(this IServiceCollection services)
    {
        services.AddSingleton<IncrementalIdGenerator>();
        services.AddSingleton<NumberToShortStringEncoder>();
        services.AddSingleton<ShortUrlCache>();
        services.AddTransient<ShortUrlGenerator>();
    }
}
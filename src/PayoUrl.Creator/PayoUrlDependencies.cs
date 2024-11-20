using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
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
        // Redis ConnectionMultiplexer
        var redisConnectionString = Environment.GetEnvironmentVariable("RedisConnectionString");
        if (string.IsNullOrEmpty(redisConnectionString))
        {
            throw new InvalidOperationException("The Redis connection string is not set in the environment variables.");
        }

        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnectionString));
    }

    private static void AddCosmos(this IServiceCollection services)
    {
        // Cosmos DB Container
        var cosmosConnectionString = Environment.GetEnvironmentVariable("CosmosConnectionString");
        services.AddSingleton<Container>(serviceProvider =>
        {
            var client = new CosmosClient(cosmosConnectionString);
            var container = client.GetContainer("payoro.dev", "payourl");
            return container;
        });
    }

    private static void AddServices(this IServiceCollection services)
    {
        // Register application services
        services.AddSingleton<RedisIncrementalIdGenerator>();
        services.AddSingleton<NumberToShortStringEncoder>();
        services.AddTransient<ShortUrlGenerator>();
    }
}
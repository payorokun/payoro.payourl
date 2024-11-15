using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

[assembly: FunctionsStartup(typeof(PayoUrl.Creator.Startup))]

namespace PayoUrl.Creator;

public class Startup : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        var redisConnectionString = Environment.GetEnvironmentVariable("RedisConnectionString");
        if (string.IsNullOrEmpty(redisConnectionString))
        {
            throw new InvalidOperationException("The Redis connection string is not set in the environment variables.");
        }
        // Minimal setup for ConnectionMultiplexer
        builder.Services.AddSingleton(_ => ConnectionMultiplexer.Connect(redisConnectionString));

        // Register any additional dependencies here
        builder.Services.AddSingleton<RedisIncrementalIdGenerator>();
    }
}
using Microsoft.Azure.Cosmos;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/init-cosmos", async () =>
{
    const string databaseId = "payoro.dev";
    const string containerId = "payourl";

    var cosmosConnectionString = Environment.GetEnvironmentVariable("CosmosConnectionString");
    var client = new CosmosClient(cosmosConnectionString);

    var database = await client.CreateDatabaseIfNotExistsAsync(databaseId);
    Console.WriteLine($"Database ready: {database.Database.Id}");

    var container = await database.Database.CreateContainerIfNotExistsAsync(containerId, "partitionKey");
    Console.WriteLine($"Container ready: {container.Container.Id}");
    
    return new { database = database.Database.Id, container = container.Container.Id };
});

app.Run();

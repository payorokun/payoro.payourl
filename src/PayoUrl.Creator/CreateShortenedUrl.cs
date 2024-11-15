using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace PayoUrl.Creator;

public class CreateShortenedUrl(ILogger<CreateShortenedUrl> logger, ShortUrlGenerator shortUrlGenerator)
{
    [Function("CreateShortUrl")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
    {
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var request = JsonConvert.DeserializeObject<UrlRequest>(requestBody);

        var shortenedUrl = shortUrlGenerator.Create(request.LongUrl);
        logger.LogInformation("C# HTTP trigger function processed a request.");
        return new OkObjectResult(shortenedUrl);
    }
}


public class UrlRequest
{
    public string LongUrl { get; set; }
}

public class UrlEntry
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("longUrl")]
    public string LongUrl { get; set; }

    [JsonProperty("createdAt")]
    public DateTime CreatedAt { get; set; }
}
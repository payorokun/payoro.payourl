using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PayoUrl.Creator.Models;
using PayoUrl.Creator.Services;

namespace PayoUrl.Creator;

public class UrlShortenerFunction(ILogger<UrlShortenerFunction> logger, ShortUrlGenerator shortUrlGenerator)
{
    [Function("CreateShortUrl")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest httpRequest)
    {
        logger.LogInformation("UrlShortenerFunction HTTP trigger function processed a request.");

        var response = await ProcessRequest(httpRequest);

        return new OkObjectResult(response);
    }

    private async Task<ShortenedUrlResponse> ProcessRequest(HttpRequest req)
    {
        var requestEntity = await ParseRequest(req);

        var shortenedUrl = await shortUrlGenerator.GenerateAsync(requestEntity.LongUrl);
        var redirectUrl = GetRedirectUrl(shortenedUrl.ShortCode, req);
        var response = new ShortenedUrlResponse(shortenedUrl.ShortCode, redirectUrl, requestEntity.LongUrl);

        logger.LogInformation("Response: {id}, {redirectUrl}, {longUrl}", response.Id, response.ShortUrl,
            response.LongUrl);

        return response;
    }

    private static async Task<CreateShorUrlRequest> ParseRequest(HttpRequest req)
    {
        var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var request = JsonConvert.DeserializeObject<CreateShorUrlRequest>(requestBody);
        return request;
    }

    private static string GetRedirectUrl(string id, HttpRequest req)
    {
        var baseUrl = req.GetHostUrl();
        return $"{baseUrl}/r/{id}";
    }
}
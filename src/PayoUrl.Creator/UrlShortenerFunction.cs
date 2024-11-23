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
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
    {
        logger.LogInformation("UrlShortenerFunction HTTP trigger function processed a request.");
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var request = JsonConvert.DeserializeObject<UrlRequest>(requestBody);

        var shortenedUrl = await shortUrlGenerator.CreateAsync(request.LongUrl);
        var redirectUrl = GetRedirectUrl(shortenedUrl.Id, req);
        var response = new ShortenedUrlResponse(shortenedUrl.Id, redirectUrl, request.LongUrl);
        logger.LogInformation("Response: {id}, {redirectUrl}, {longUrl}", response.Id, response.ShortUrl, response.LongUrl);
        return new OkObjectResult(response);
    }

    private string GetRedirectUrl(string id, HttpRequest req)
    {
        var baseUrl = req.GetHostUrl();
        return $"{baseUrl}/r/{id}";
    }
}
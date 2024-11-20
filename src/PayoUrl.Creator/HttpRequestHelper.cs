using Microsoft.AspNetCore.Http;

namespace PayoUrl.Creator;

public static class HttpRequestHelper
{
    public static string GetHostUrl(this HttpRequest req)
    {
        var uri = new UriBuilder
        {
            Scheme = req.Scheme,
            Host = req.Host.Host,
            Path = req.PathBase
        };

        if (req.Host.Port.HasValue)
        {
            uri.Port = req.Host.Port.Value;
        }

        return uri.Uri.ToString();
    }
}
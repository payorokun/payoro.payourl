using Newtonsoft.Json;

namespace PayoUrl.Creator.Models;

public record ShortenedUrlEntity
{
    [JsonProperty("shortCode")]
    public string ShortCode { get; set; }

    [JsonProperty("longUrl")]
    public string LongUrl { get; set; }

    [JsonProperty("createdAt")]
    public DateTime CreatedAt { get; set; }
}
using Newtonsoft.Json;

namespace PayoUrl.Creator.Models;

public record UrlEntry
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("longUrl")]
    public string LongUrl { get; set; }

    [JsonProperty("createdAt")]
    public DateTime CreatedAt { get; set; }
}
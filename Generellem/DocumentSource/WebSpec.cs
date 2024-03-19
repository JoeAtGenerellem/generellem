using System.Text.Json.Serialization;

namespace Generellem.DocumentSource;

public class WebSpec
{
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

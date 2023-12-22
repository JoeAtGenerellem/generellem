using System.Text.Json.Serialization;

namespace Generellem.DocumentSource;

public class WebSpec
{
    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

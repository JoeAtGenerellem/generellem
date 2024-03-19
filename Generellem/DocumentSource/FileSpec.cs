using System.Text.Json.Serialization;

namespace Generellem.DocumentSource;

public class FileSpec
{
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("path")]
    public string? Path { get; set; }
}

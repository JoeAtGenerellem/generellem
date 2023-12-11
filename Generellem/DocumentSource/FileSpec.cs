using System.Text.Json.Serialization;

namespace Generellem.DocumentSource;

public class FileSpec
{
    [JsonPropertyName("path")]
    public string? Path { get; set; }
}

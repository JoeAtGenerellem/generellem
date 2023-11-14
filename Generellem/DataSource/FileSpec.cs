using System.Text.Json.Serialization;

namespace Generellem.DataSource;

public class FileSpec
{
    [JsonPropertyName("path")]
    public string? Path { get; set; }
}

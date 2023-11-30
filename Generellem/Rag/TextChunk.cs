
namespace Generellem.Rag;

public class TextChunk
{
    public string ID { get; set; } = Guid.NewGuid().ToString();
    public string Content { get; set; } = string.Empty;
    public ReadOnlyMemory<float> Embedding { get; set; }
    public string FileRef { get; set; } = string.Empty;
}

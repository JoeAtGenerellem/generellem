
namespace Generellem.Rag;

public class TextChunk
{
    public virtual string ID { get; set; } = Guid.NewGuid().ToString();
    public virtual string Content { get; set; } = string.Empty;
    public virtual ReadOnlyMemory<float> Embedding { get; set; }
    public virtual string FileRef { get; set; } = string.Empty;
}

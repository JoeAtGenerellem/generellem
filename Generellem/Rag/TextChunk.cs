
namespace Generellem.Rag;

public class TextChunk
{
    public virtual string? ID { get; set; }
    public virtual string? Content { get; set; }
    public virtual ReadOnlyMemory<float> Embedding { get; set; }
    public virtual string? DocumentReference { get; set; }
}

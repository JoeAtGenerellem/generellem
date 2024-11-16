namespace Generellem.Embedding;

public class TextChunk
{
    public virtual string? ID { get; set; }
    public virtual string? DocumentReference { get; set; }
    public virtual string? Content { get; set; }
    public virtual ReadOnlyMemory<float> Embedding { get; set; }
    public virtual int Order { get; set; }
    public virtual string? SourceReference { get; set; }
}

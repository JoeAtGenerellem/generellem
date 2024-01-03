namespace Generellem.Rag.Tests;

public class TextProcessorTests
{
    [Fact]
    public void BreakIntoChunks_WithShortText_ReturnsOneChunk()
    {
        string? text = "Hello world";
        string? fileRef = "file1.txt";

        List<TextChunk> chunks = TextProcessor.BreakIntoChunks(text, fileRef);

        Assert.Single(chunks);
    }

    [Fact]
    public void BreakIntoChunks_WithLongText_ReturnsMultipleChunks()
    {
        string? text = new('a', 6000);
        string? fileRef = "file1.txt";

        List<TextChunk> chunks = TextProcessor.BreakIntoChunks(text, fileRef);

        Assert.True(chunks.Count > 1);
    }

    [Fact]
    public void BreakIntoChunks_WithLongText_ReturnsAppropriatelySizedChunks()
    {
        const int FirstChunkSize = 5000;
        const int SecondChunkSize = 1100;

        string? text = new('a', 6000);
        string? fileRef = "file1.txt";

        List<TextChunk> chunks = TextProcessor.BreakIntoChunks(text, fileRef);

        Assert.Equal(FirstChunkSize, chunks[0]?.Content?.Length);
        Assert.Equal(SecondChunkSize, chunks[1]?.Content?.Length);
    }

    [Fact]
    public void BreakIntoChunks_WithLongText_ReturnsOverlappedText()
    {
        string? text = new string('a', 4995) + "1234567890" + new string('b', 995);
        string? fileRef = "file1.txt";

        List<TextChunk> chunks = TextProcessor.BreakIntoChunks(text, fileRef);

        Assert.Equal(chunks[0]?.Content?[^100..], chunks[1]?.Content?[..100]);
    }

    [Fact]
    public void BreakIntoChunks_ChunksHaveCorrectContent()
    {
        string? text = new string('a', 4995) + "1234567890" + new string('b', 995);
        string? fileRef = "file1.txt";

        List<TextChunk> chunks = TextProcessor.BreakIntoChunks(text, fileRef);

        Assert.Equal(text, chunks[0]?.Content?[..^100] + chunks[1].Content);
    }
}

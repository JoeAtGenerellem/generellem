namespace Generellem.Rag.Tests;

public class TextProcessorTests
{
    [Fact]
    public void BreakIntoChunks_WithShortText_ReturnsOneChunk()
    {
        string? text = "Hello world";
        string? fileRef = "file1.txt";

        var result = TextProcessor.BreakIntoChunks(text, fileRef);

        Assert.Single(result);
    }

    [Fact]
    public void BreakIntoChunks_WithLongText_ReturnsMultipleChunks()
    {
        string? text = new('a', 6000);
        string? fileRef = "file1.txt";

        var result = TextProcessor.BreakIntoChunks(text, fileRef);

        Assert.True(result.Count > 1);
    }

    [Fact]
    public void BreakIntoChunks_WithLongText_ReturnsAppropriatelySizedChunks()
    {
        const int FirstChunkSize = 5000;
        const int SecondChunkSize = 1100;

        string? text = new('a', 6000);
        string? fileRef = "file1.txt";

        var result = TextProcessor.BreakIntoChunks(text, fileRef);

        Assert.Equal(FirstChunkSize, result[0].Content.Length);
        Assert.Equal(SecondChunkSize, result[1].Content.Length);
    }

    [Fact]
    public void BreakIntoChunks_WithLongText_ReturnsOverlappedText()
    {
        string? text = new string('a', 4995) + "1234567890" + new string('b', 995);
        string? fileRef = "file1.txt";

        var result = TextProcessor.BreakIntoChunks(text, fileRef);

        Assert.Equal(result[0].Content[^100..], result[1].Content[..100]);
    }

    [Fact]
    public void BreakIntoChunks_ChunksHaveCorrectContent()
    {
        string? text = new string('a', 4995) + "1234567890" + new string('b', 995);
        string? fileRef = "file1.txt";

        var result = TextProcessor.BreakIntoChunks(text, fileRef);

        Assert.Equal(text, result[0].Content[..^100] + result[1].Content);
    }
}

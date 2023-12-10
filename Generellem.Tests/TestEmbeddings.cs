namespace Generellem.Tests;

internal class TestEmbeddings
{
    const int EmbeddingVectorSize = 1536;

    public static float[] CreateEmbeddingArray() => Enumerable.Range(1, EmbeddingVectorSize).Select(i => i * 1f).ToArray();
}

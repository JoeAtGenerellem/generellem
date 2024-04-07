using Azure.AI.OpenAI;

using Generellem.Document.DocumentTypes;
using Generellem.Rag;

namespace Generellem.Embedding;

public interface IEmbedding
{
    /// <summary>
    /// Takes a raw document, turns it into text, and creates an embedding.
    /// </summary>
    /// <param name="fullText">Document to embed.</param>
    /// <param name="docType">An <see cref="IDocumentType"/> for turning the document into text.</param>
    /// <param name="fileName">Name of file in embedding.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <returns><see cref="List{T}"/> of <see cref="TextChunk"/>, which holds both the real and embedded content.</returns>
    Task<List<TextChunk>> EmbedAsync(string fullText, IDocumentType docType, string fileName, CancellationToken cancellationToken);

    /// <summary>
    /// Embedding Options for Azure Search.
    /// </summary>
    /// <param name="text">Text string for calculating options.</param>
    /// <returns><see cref="EmbeddingsOptions"/></returns>
    EmbeddingsOptions GetEmbeddingOptions(string text);
}

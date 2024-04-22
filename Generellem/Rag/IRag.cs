using Azure.AI.OpenAI;

using Generellem.Embedding;
using Generellem.Llm;

namespace Generellem.Rag;

/// <summary>
/// Supports Retrieval Augmented Generation (RAG).
/// </summary>
/// <remarks>
/// The assumption is that all embedding, indexing, and searching happens here.
/// This should be the minimal amount of processing required.
/// </remarks>
public interface IRag
{
    /// <summary>
    /// Instructions to the LLM on how interpret query and respond.
    /// </summary>
    string SystemMessage { get; set; }

    /// <summary>
    /// Tradeoff in accuracy vs creativity.
    /// </summary>
    /// <remarks>
    /// Range can vary, depending on model.
    /// e.g. OpenAI range is 0 to 2 but
    /// Mistrial range is 0 to 1
    /// </remarks>
    float Temperature { get; set; }

    /// <summary>
    /// Builds a request based on prompt content.
    /// </summary>
    /// <typeparam name="TRequest">Type of request for LLM.</typeparam>
    /// <param name="requestText">Text from user.</param>
    /// <param name="chatHistory">Previous queries in this thread.</param>
    /// <param name="cancelToken"><see cref="CancellationToken"/></param>
    /// 
    /// <returns>Full request that can be sent to the LLM.</returns>
    Task<TRequest> BuildRequestAsync<TRequest>(string requestText, Queue<ChatRequestUserMessage> chatHistory, CancellationToken cancelToken)
        where TRequest : IChatRequest, new();

    /// <summary>
    /// Performs Vector Search for chunks matching given text.
    /// </summary>
    /// <param name="text">Text for searching for matches.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <returns>List of <see cref="TextChunk"> matching query.</returns>
    Task<List<TextChunk>> SearchAsync(string text, CancellationToken cancellationToken);
}

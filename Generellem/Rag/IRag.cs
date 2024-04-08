using Azure.AI.OpenAI;

using Generellem.Llm;
using Generellem.Llm.AzureOpenAI;

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
    /// Builds a request based on prompt content.
    /// </summary>
    /// <typeparam name="TRequest">Type of request for LLM.</typeparam>
    /// <param name="systemMessage">Instructions to the LLM on how to process the request.</param>
    /// <param name="requestText">Text from user.</param>
    /// <param name="chatHistory">Previous queries in this thread.</param>
    /// <param name="cancelToken"><see cref="CancellationToken"/></param>
    /// <returns>Full request that can be sent to the LLM.</returns>
    Task<TRequest> BuildRequestAsync<TRequest>(string systemMessage, string requestText, Queue<ChatMessage> chatHistory, CancellationToken cancelToken)
        where TRequest : IChatRequest, new();

    /// <summary>
    /// Performs Vector Search for chunks matching given text.
    /// </summary>
    /// <param name="text">Text for searching for matches.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <returns>List of text chunks matching query.</returns>
    Task<List<string>> SearchAsync(string text, CancellationToken cancellationToken);
}

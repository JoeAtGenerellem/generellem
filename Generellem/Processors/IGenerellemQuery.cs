using Azure.AI.OpenAI;

using Generellem.Services;

namespace Generellem.Processors;

/// <summary>
/// Implement this interface any time you need to implement a query for 
/// a new LLM, see <see cref="AzureOpenAIQuery"/> for an example.
/// </summary>
public interface IGenerellemQuery
{
    /// <summary>
    /// Instructions to the LLM on how interpret query and respond.
    /// </summary>
    string SystemMessage { get; set; }
    IDynamicConfiguration Configuration { get; }

    /// <summary>
    /// Performs whatever process you need to prepare a user's text and handle the response
    /// </summary>
    /// <param name="queryText">User's request</param>
    /// <param name="cancelToken"><see cref="CancellationToken"/></param>
    /// <param name="chatHistory">History of questions asked to add to context</param>
    /// <returns>LLM response</returns>
    Task<string> AskAsync(string queryText, Queue<ChatMessage> chatHistory, CancellationToken cancelToken);
}
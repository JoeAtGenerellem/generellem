using Generellem.Llm;
using Generellem.Llm.AzureOpenAI;
using Generellem.Rag;

using OpenAI.Chat;

namespace Generellem.Processors;

/// <summary>
/// Orchestrates Retrieval-Augmented Generation (RAG)
/// </summary>
public class AzureOpenAIQuery(ILlm llm, IRag rag) : IGenerellemQuery
{
    /// <summary>
    /// Searches for context, builds a prompt, and gets a response from Azure OpenAI
    /// </summary>
    /// <typeparam name="TResponse">Type of response with all data returned from the LLM.</typeparam>
    /// <param name="queryText">User's request</param>
    /// <param name="chatHistory">History of questions asked/answered to add to context</param>
    /// <param name="cancelToken"><see cref="CancellationToken"/></param>
    /// <returns>Azure OpenAI response text</returns>
    public async Task<string> AskAsync(string queryText, Queue<ChatMessage> chatHistory, CancellationToken cancelToken)
    {
        QueryDetails<AzureOpenAIChatRequest, AzureOpenAIChatResponse> response = 
            await PromptAsync<AzureOpenAIChatRequest, AzureOpenAIChatResponse>(queryText, chatHistory, cancelToken);

        return response.Response?.Text ?? string.Empty;
    }

    /// <summary>
    /// Searches for context, builds a prompt, and gets a response from Azure OpenAI
    /// </summary>
    /// <typeparam name="TResponse">Type of response with all data returned from the LLM.</typeparam>
    /// <param name="queryText">User's request</param>
    /// <param name="chatHistory">History of questions asked/answered to add to context</param>
    /// <param name="cancelToken"><see cref="CancellationToken"/></param>
    /// <returns>Azure OpenAI response</returns>
    public virtual async Task<QueryDetails<TRequest, TResponse>> PromptAsync<TRequest, TResponse>(
        string queryText, Queue<ChatMessage> chatHistory, CancellationToken cancelToken)
        where TRequest : IChatRequest
        where TResponse : IChatResponse
    {
        AzureOpenAIChatRequest request = 
            await rag.BuildRequestAsync<AzureOpenAIChatRequest>(queryText, chatHistory, cancelToken);

        AzureOpenAIChatResponse response = await llm.PromptAsync<AzureOpenAIChatResponse>(request, cancelToken);

        return new QueryDetails<TRequest, TResponse>
        {
            Request = (TRequest)(IChatRequest)request,
            Response = (TResponse)(IChatResponse) response
        };
    }
}

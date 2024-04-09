using Azure;
using Azure.AI.OpenAI;
using Azure.Core;

using Generellem.Llm;
using Generellem.Llm.AzureOpenAI;
using Generellem.Rag;
using Generellem.Services;

namespace Generellem.Processors;

/// <summary>
/// Orchestrates Retrieval-Augmented Generation (RAG)
/// </summary>
public class AzureOpenAIQuery(ILlm llm, IRag rag) : IGenerellemQuery
{
    /// <summary>
    /// Instructions to the LLM on how interpret query and respond.
    /// </summary>
    public string SystemMessage { get; set; } =
        "You are a professional AI bot that returns accurate content for busy workers.\n" +
        "Please answer the user's question using only information you can find in the context.\n" +
        "If the user's question is unrelated to the information in the context, say you don't know.\n";

    /// <summary>
    /// Searches for context, builds a prompt, and gets a response from Azure OpenAI
    /// </summary>
    /// <typeparam name="TResponse">Type of response with all data returned from the LLM.</typeparam>
    /// <param name="requestText">User's request</param>
    /// <param name="chatHistory">History of questions asked to add to context</param>
    /// <param name="cancelToken"><see cref="CancellationToken"/></param>
    /// <returns>Azure OpenAI response text</returns>
    public async Task<string> AskAsync(string queryText, Queue<ChatMessage> chatHistory, CancellationToken cancelToken)
    {
        QueryDetails<AzureOpenAIChatRequest, AzureOpenAIChatResponse> response = 
            await PromptAsync<AzureOpenAIChatRequest, AzureOpenAIChatResponse>(queryText, chatHistory, cancelToken);

        return response.Request?.Text ?? string.Empty;
    }

    /// <summary>
    /// Searches for context, builds a prompt, and gets a response from Azure OpenAI
    /// </summary>
    /// <typeparam name="TResponse">Type of response with all data returned from the LLM.</typeparam>
    /// <param name="requestText">User's request</param>
    /// <param name="chatHistory">History of questions asked to add to context</param>
    /// <param name="cancelToken"><see cref="CancellationToken"/></param>
    /// <returns>Azure OpenAI response</returns>
    public virtual async Task<QueryDetails<TRequest, TResponse>> PromptAsync<TRequest, TResponse>(
        string requestText, Queue<ChatMessage> chatHistory, CancellationToken cancelToken)
        where TRequest : IChatRequest
        where TResponse : IChatResponse
    {
        AzureOpenAIChatRequest request = 
            await rag.BuildRequestAsync<AzureOpenAIChatRequest>(SystemMessage, requestText, chatHistory, cancelToken);

        AzureOpenAIChatResponse response = await llm.PromptAsync<AzureOpenAIChatResponse>(request, cancelToken);

        return new QueryDetails<TRequest, TResponse>
        {
            Request = (TRequest)(IChatRequest)request,
            Response = (TResponse)(IChatResponse) response
        };
    }
}

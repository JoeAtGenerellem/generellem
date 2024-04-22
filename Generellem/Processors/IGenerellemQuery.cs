﻿using Azure.AI.OpenAI;

using Generellem.Llm;

namespace Generellem.Processors;

/// <summary>
/// Implement this interface any time you need to implement a query for 
/// a new LLM, see <see cref="AzureOpenAIQuery"/> for an example.
/// </summary>
public interface IGenerellemQuery
{
    /// <summary>
    /// Performs whatever process you need to prepare a user's text and handle the response
    /// </summary>
    /// <param name="queryText">User's request</param>
    /// <param name="cancelToken"><see cref="CancellationToken"/></param>
    /// <param name="chatHistory">History of questions asked to add to context</param>
    /// <returns>LLM response</returns>
    Task<string> AskAsync(string queryText, Queue<ChatRequestUserMessage> chatHistory, CancellationToken cancelToken);

    /// <summary>
    /// Performs whatever process you need to prepare a user's text and handle the response
    /// </summary>
    /// <typeparam name="TResponse">Type of response with all data returned from the LLM.</typeparam>
    /// <param name="queryText">User's request</param>
    /// <param name="cancelToken"><see cref="CancellationToken"/></param>
    /// <param name="chatHistory">History of questions asked to add to context</param>
    /// <returns>LLM response</returns>
    Task<QueryDetails<TRequest, TResponse>> PromptAsync<TRequest, TResponse>(string requestText, Queue<ChatRequestUserMessage> chatHistory, CancellationToken cancelToken)
        where TRequest : IChatRequest
        where TResponse : IChatResponse;
}
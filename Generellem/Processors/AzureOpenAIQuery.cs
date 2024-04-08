using System.Text;

using Azure.AI.OpenAI;

using Generellem.Llm;
using Generellem.Llm.AzureOpenAI;
using Generellem.Rag;
using Generellem.Services;

namespace Generellem.Processors;

/// <summary>
/// Orchestrates Retrieval-Augmented Generation (RAG)
/// </summary>
/// <remarks>
/// Inspired by Retrieval-Augmented Generation (RAG)/Bea Stollnitz at https://bea.stollnitz.com/blog/rag/
/// </remarks>
public class AzureOpenAIQuery(IDynamicConfiguration config, IGenerellemIngestion ingestion, ILlm llm/*, IRag rag*/) : IGenerellemQuery
{
    /// <summary>
    /// We use this to summarize the user query, based on recent context.
    /// </summary>
    string ContextMessage { get; set; } =
        "You're an AI assistant reading the transcript of a conversation " +
        "between a user and an assistant. Given the chat history and " +
        "user's query, infer the user's real intent.";

    /// <summary>
    /// Instructions to the LLM on how interpret query and respond.
    /// </summary>
    public string SystemMessage { get; set; } =
        "You are a professional AI bot that returns accurate content for busy workers.\n" +
        "Please answer the user's question using only information you can find in the context.\n" +
        "If the user's question is unrelated to the information in the context, say you don't know.\n";

    public IDynamicConfiguration Configuration => config;

    /// <summary>
    /// Searches for context, builds a prompt, and gets a response from Azure OpenAI
    /// </summary>
    /// <param name="requestText">User's request</param>
    /// <param name="cancelToken"><see cref="CancellationToken"/></param>
    /// <param name="chatHistory">History of questions asked to add to context</param>
    /// <returns>Azure OpenAI response</returns>
    /// <exception cref="ArgumentNullException">Throws if config values not found</exception>
    public virtual async Task<string> AskAsync(string requestText, Queue<ChatMessage> chatHistory, CancellationToken cancelToken)
    {
        string? deploymentName = config[GKeys.AzOpenAIDeploymentName];
        ArgumentException.ThrowIfNullOrWhiteSpace(deploymentName, nameof(deploymentName));

        string userIntent = await SummarizeUserIntentAsync(requestText, chatHistory, deploymentName, cancelToken);

        List<string> matchingDocuments = await ingestion.SearchAsync(userIntent, cancelToken);

        string context =
            "\nContext: \n\n" +
            "```" +
            string.Join("\n\n", matchingDocuments) +
            "```\n";

        ChatMessage userQuery = new(ChatRole.User, requestText);

        List<ChatMessage> messages =
        [
            new ChatMessage(ChatRole.System, SystemMessage + context),
            userQuery
        ];

        ManageChatHistory(chatHistory, userQuery);

        ChatCompletionsOptions chatCompletionOptions = new(deploymentName, messages);
        AzureOpenAIChatRequest request = new(chatCompletionOptions);

        AzureOpenAIChatResponse lastResponse = await llm.AskAsync<AzureOpenAIChatResponse>(request, cancelToken);

        return lastResponse.Text ?? string.Empty;
    }

    /// <summary>
    /// Ensures the latest queries reside in chat history and that chat history doesn't exceed a specified window size.
    /// </summary>
    /// <param name="chatHistory"><see cref="Queue{T}"/> of <see cref="ChatMessage"/>, representing the current chat history.</param>
    /// <param name="userQuery"><see cref="ChatMessage"/> with the most recent user query to add to history.</param>
    protected virtual void ManageChatHistory(Queue<ChatMessage> chatHistory, ChatMessage userQuery)
    {
        const int ChatHistorySize = 5;

        while (chatHistory.Count >= ChatHistorySize)
            chatHistory.Dequeue();

        chatHistory.Enqueue(userQuery);
    }

    /// <summary>
    /// Asks the LLM to clarify what the user wants to accomplish based on current query and chat history.
    /// </summary>
    /// <param name="userQuery">The question that the user is asking.</param>
    /// <param name="chatHistory">Context with previous questions the user asked.</param>
    /// <param name="deploymentName">Name of deployed LLM that we're using.</param>
    /// <param name="cancelToken"><see cref="CancellationToken"/></param>
    /// <returns>Clarification of user query, based on context.</returns>
    protected virtual async Task<string> SummarizeUserIntentAsync(string userQuery, Queue<ChatMessage> chatHistory, string deploymentName, CancellationToken cancelToken)
    {
        StringBuilder sb = new();

        foreach (ChatMessage chatMessage in chatHistory)
            sb.AppendLine($"{chatMessage.Role}: {chatMessage.Content}\n");

        List<ChatMessage> messages =
        [
            new ChatMessage(
                ChatRole.System,
                ContextMessage +
                $"\n\nChat History: {sb}" +
                $"\n\nUser's query: {userQuery}")
        ];

        ChatCompletionsOptions chatCompletionOptions = new(deploymentName, messages);
        AzureOpenAIChatRequest request = new(chatCompletionOptions);

        AzureOpenAIChatResponse lastResponse = await llm.AskAsync<AzureOpenAIChatResponse>(request, cancelToken);

        return lastResponse.Text ?? string.Empty;
    }
}

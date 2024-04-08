using Azure.AI.OpenAI;
using Azure;

using Generellem.Services;
using Generellem.Services.Exceptions;

using Polly;
using Generellem.Embedding;
using Generellem.Llm;
using Generellem.Services.Azure;
using Microsoft.Extensions.Logging;
using Generellem.Llm.AzureOpenAI;
using System.Text;

namespace Generellem.Rag.AzureOpenAI;

/// <summary>
/// Performs Retrieval-Augmented Generation (RAG) for Azure OpenAI.
/// </summary>
public class AzureOpenAIRag(
    IAzureSearchService azSearchSvc,
    IDynamicConfiguration config,
    IEmbedding embedding,
    LlmClientFactory llmClientFact,
    ILlm llm,
    ILogger<AzureOpenAIRag> logger)
    : IRag
{
    /// <summary>
    /// We use this to summarize the user query, based on recent context.
    /// </summary>
    string ContextMessage { get; set; } =
        "You're an AI assistant reading the transcript of a conversation " +
        "between a user and an assistant. Given the chat history and " +
        "user's query, infer the user's real intent.";

    readonly OpenAIClient openAIClient = llmClientFact.CreateOpenAIClient();

    readonly ResiliencePipeline pipeline = 
        new ResiliencePipelineBuilder()
            .AddRetry(new()
             {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(ex => ex is not GenerellemNeedsIngestionException)
             })
            .AddTimeout(TimeSpan.FromSeconds(7))
            .Build();

    /// <summary>
    /// Builds a request based on prompt content.
    /// </summary>
    /// <param name="systemMessage">Instructions to the LLM on how to process the request.</param>
    /// <param name="requestText">Text from user.</param>
    /// <param name="chatHistory">Previous queries in this thread.</param>
    /// <param name="cancelToken"><see cref="CancellationToken"/></param>
    /// <returns>Full request that can be sent to the LLM.</returns>
    public async Task<TRequest> BuildRequestAsync<TRequest>(string systemMessage, string requestText, Queue<ChatMessage> chatHistory, CancellationToken cancelToken)
        where TRequest : IChatRequest, new()
    {
        string? deploymentName = config[GKeys.AzOpenAIDeploymentName];
        ArgumentException.ThrowIfNullOrWhiteSpace(deploymentName, nameof(deploymentName));

        string userIntent = await SummarizeUserIntentAsync(requestText, chatHistory, deploymentName, cancelToken);

        List<string> matchingDocuments = await SearchAsync(userIntent, cancelToken);

        string context =
            "\nContext: \n\n" +
            "```" +
            string.Join("\n\n", matchingDocuments) +
            "```\n";

        ChatMessage userQuery = new(ChatRole.User, requestText);

        List<ChatMessage> messages =
        [
            new ChatMessage(ChatRole.System, systemMessage + context),
            userQuery
        ];

        ManageChatHistory(chatHistory, userQuery);

        ChatCompletionsOptions chatCompletionOptions = new(deploymentName, messages);
        return (TRequest)(IChatRequest) new AzureOpenAIChatRequest() { Options = chatCompletionOptions };
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
        AzureOpenAIChatRequest request = new() { Options = chatCompletionOptions };

        AzureOpenAIChatResponse lastResponse = await llm.PromptAsync<AzureOpenAIChatResponse>(request, cancelToken);

        return lastResponse.Text ?? string.Empty;
    }

    /// <summary>
    /// Performs Vector Search for chunks matching given text.
    /// </summary>
    /// <param name="text">Text for searching for matches.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <returns>List of text chunks matching query.</returns>
    public virtual async Task<List<string>> SearchAsync(string text, CancellationToken cancellationToken)
    {
        EmbeddingsOptions embeddingsOptions = embedding.GetEmbeddingOptions(text);

        try
        {
            Response<Embeddings> embeddings = await pipeline.ExecuteAsync<Response<Embeddings>>(
            async token => await openAIClient.GetEmbeddingsAsync(embeddingsOptions, token),
                cancellationToken);

            ReadOnlyMemory<float> embedding = embeddings.Value.Data[0].Embedding;
            List<TextChunk> chunks = await pipeline.ExecuteAsync(
                async token => await azSearchSvc.SearchAsync<TextChunk>(embedding, token),
                cancellationToken);

            return
                (from chunk in chunks
                 select chunk.Content)
                .ToList();
        }
        catch (RequestFailedException rfEx)
        {
            logger.LogError(GenerellemLogEvents.AuthorizationFailure, rfEx, "Please check credentials and exception details for more info.");
            throw;
        }
    }
}

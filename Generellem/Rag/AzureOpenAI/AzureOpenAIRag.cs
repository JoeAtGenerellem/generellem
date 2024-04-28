using System.Text;

using Azure;
using Azure.AI.OpenAI;

using Generellem.Embedding;
using Generellem.Llm;
using Generellem.Llm.AzureOpenAI;
using Generellem.Processors;
using Generellem.Services;
using Generellem.Services.Azure;
using Generellem.Services.Exceptions;

using Microsoft.Extensions.Logging;

using Polly;

namespace Generellem.Rag.AzureOpenAI;

/// <summary>
/// Performs Retrieval-Augmented Generation (RAG) for Azure OpenAI.
/// </summary>
/// <remarks>
/// Inspired by Retrieval-Augmented Generation (RAG)/Bea Stollnitz at https://bea.stollnitz.com/blog/rag/
/// </remarks>
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

    /// <summary>
    /// Instructions to the LLM on how interpret query and respond.
    /// </summary>
    public string SystemMessage { get; set; } =
        "You are a professional AI bot that returns accurate content for busy workers.\n" +
        "Please answer the user's question using only information you can find in the context.\n" +
        "If the user's question is unrelated to the information in the context, say you don't know.\n";

    /// <summary>
    /// Tradeoff in accuracy vs creativity - ranges from 0 to 2 in OpenAI
    /// </summary>
    public float Temperature { get; set; } = 0;

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
    /// <param name="requestText">Text from user.</param>
    /// <param name="chatHistory">Previous queries in this thread.</param>
    /// <param name="cancelToken"><see cref="CancellationToken"/></param>
    /// <returns>Full request that can be sent to the LLM.</returns>
    public async Task<TRequest> BuildRequestAsync<TRequest>(string requestText, Queue<ChatRequestMessage> chatHistory, CancellationToken cancelToken)
        where TRequest : IChatRequest, new()
    {
        AzureOpenAIChatRequest request = new();

        string? deploymentName = config[GKeys.AzOpenAIDeploymentName];
        ArgumentException.ThrowIfNullOrWhiteSpace(deploymentName, nameof(deploymentName));

        request.SummarizedUserIntent = await SummarizeUserIntentAsync(requestText, chatHistory, deploymentName, cancelToken);
        string summarizedIntentMessage = request.SummarizedUserIntent.Response?.Text ?? string.Empty;

        string context = await BuildContext(request, summarizedIntentMessage, cancelToken);

        ChatRequestUserMessage userQuery = new(requestText);

        List<ChatRequestMessage> messages =
        [
            new ChatRequestSystemMessage(SystemMessage + "\n" + context),
            userQuery
        ];

        request.Options =
            new ChatCompletionsOptions(deploymentName, messages)
            {
                Temperature = Temperature
            };

        return (TRequest)(IChatRequest)request;
    }

    protected virtual async Task<string> BuildContext(AzureOpenAIChatRequest request, string summarizedIntentMessage, CancellationToken cancelToken)
    {
        request.TextChunks = await SearchAsync(summarizedIntentMessage, cancelToken);

        foreach (TextChunk chunk in request.TextChunks)
            chunk.Embedding = null;

        string context =
            "\nContext: \n\n" +
            "```" +
            string.Join("\n\n", request.TextChunks.Select(c => c.Content)) +
            "```\n";
        return context;
    }

    /// <summary>
    /// Asks the LLM to clarify what the user wants to accomplish based on current query and chat history.
    /// </summary>
    /// <param name="userQuery">The question that the user is asking.</param>
    /// <param name="chatHistory">Context with previous questions the user asked.</param>
    /// <param name="deploymentName">Name of deployed LLM that we're using.</param>
    /// <param name="cancelToken"><see cref="CancellationToken"/></param>
    /// <returns><see cref="QueryDetails{TRequest, TResponse}"/> including clarification of user query, based on context.</returns>
    protected virtual async Task<QueryDetails<AzureOpenAIChatRequest, AzureOpenAIChatResponse>> SummarizeUserIntentAsync(
        string userQuery, Queue<ChatRequestMessage> chatHistory, string deploymentName, CancellationToken cancelToken)
    {
        StringBuilder sb = new();

        foreach (ChatRequestMessage chatMessage in chatHistory)
            sb.AppendLine($"{chatMessage.Role}: {AzureOpenAIChatRequest.GetRequestContent(chatMessage)}\n");

        List<ChatRequestSystemMessage> messages =
        [
            new ChatRequestSystemMessage(
                ContextMessage +
                $"\n\nChat History: {sb}" +
                $"\n\nUser's query: {userQuery}")
        ];

        ChatCompletionsOptions chatCompletionOptions = new(deploymentName, messages);
        AzureOpenAIChatRequest request = new() { Options = chatCompletionOptions };

        AzureOpenAIChatResponse lastResponse = await llm.PromptAsync<AzureOpenAIChatResponse>(request, cancelToken);

        return new QueryDetails<AzureOpenAIChatRequest, AzureOpenAIChatResponse>()
        {
            Request = request,
            Response = lastResponse
        };
    }

    /// <summary>
    /// Performs Vector Search for chunks matching given text.
    /// </summary>
    /// <param name="text">Text for searching for matches.</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <returns>List of text chunks matching query.</returns>
    public virtual async Task<List<TextChunk>> SearchAsync(string text, CancellationToken cancellationToken)
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

            return chunks;
            //return
            //    (from chunk in chunks
            //     select chunk.Content)
            //    .ToList();
        }
        catch (RequestFailedException rfEx)
        {
            logger.LogError(GenerellemLogEvents.AuthorizationFailure, rfEx, "Please check credentials and exception details for more info.");
            throw;
        }
    }
}

using Azure.AI.OpenAI;

using Generellem.DocumentSource;
using Generellem.Document;
using Generellem.Document.DocumentTypes;
using Generellem.Llm;
using Generellem.Llm.AzureOpenAI;
using Generellem.Rag;
using Generellem.Services;

using Microsoft.Extensions.Configuration;
using System.Text;

namespace Generellem.Orchestrator;

/// <summary>
/// Orchestrates Retrieval-Augmented Generation (RAG)
/// </summary>
/// <remarks>
/// Inspired byRetrieval-Augmented Generation (RAG)/Bea Stollnitz at https://bea.stollnitz.com/blog/rag/
/// </remarks>
public class AzureOpenAIOrchestrator(
    IConfiguration config, 
    IDocumentSource docSource, 
    ILlm llm, 
    IRag rag) 
    : GenerellemOrchestratorBase(docSource, llm, rag)
{
    readonly IConfiguration config = config;

    public virtual AzureOpenAIChatResponse? LastResponse { get; set; }

    const string ContextMessage =
        "You're an AI assistant reading the transcript of a conversation " +
        "between a user and an assistant. Given the chat history and " +
        "user's query, infer user real intent.";

    const string SystemMessage =
        "You are a professional AI bot that returns accurate content for busy workers.\n" +
        "Please answer the user's question using only information you can find in the context.\n" +
        "If the user's question is unrelated to the information in the context, say you don't know.\n";

    /// <summary>
    /// Searches for context, builds a prompt, and gets a response from Azure OpenAI
    /// </summary>
    /// <param name="requestText">User's request</param>
    /// <param name="cancelToken"><see cref="CancellationToken"/></param>
    /// <param name="chatHistory">History of questions asked to add to context</param>
    /// <returns>Azure OpenAI response</returns>
    /// <exception cref="ArgumentNullException">Throws if config values not found</exception>
    public override async Task<string> AskAsync(string requestText, Queue<ChatMessage> chatHistory, CancellationToken cancelToken)
    {
        string? deploymentName = config[GKeys.AzOpenAIDeploymentName];
        ArgumentException.ThrowIfNullOrWhiteSpace(deploymentName, nameof(deploymentName));

        string userIntent = await SummarizeUserIntentAsync(requestText, chatHistory, deploymentName, cancelToken);

        List<string> searchResponse = await Rag.SearchAsync(userIntent, cancelToken);

        string context =
            "Context: \n\n" +
            "```" +
            string.Join("\n\n", searchResponse) +
            "```\n";

        ChatMessage userQuery = new(ChatRole.User, requestText);

        List<ChatMessage> messages =
        [
            new ChatMessage(ChatRole.System, SystemMessage + context),
            userQuery
        ];

        ChatCompletionsOptions chatCompletionOptions = new(deploymentName, messages);
        AzureOpenAIChatRequest request = new(chatCompletionOptions);

        LastResponse = await Llm.AskAsync<AzureOpenAIChatResponse>(request, cancelToken);

        ManageChatHistory(chatHistory, userQuery);

        return LastResponse.Text ?? string.Empty;
    }

    static void ManageChatHistory(Queue<ChatMessage> chatHistory, ChatMessage userQuery)
    {
        const int ChatHistorySize = 5;

        while (chatHistory.Count >= ChatHistorySize)
            chatHistory.Dequeue();

        chatHistory.Enqueue(userQuery);
    }

    async Task<string> SummarizeUserIntentAsync(string requestText, Queue<ChatMessage> chatHistory, string deploymentName, CancellationToken cancelToken)
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
                $"\n\nUser's query: {requestText}")
        ];

        ChatCompletionsOptions chatCompletionOptions = new(deploymentName, messages);
        AzureOpenAIChatRequest request = new(chatCompletionOptions);

        LastResponse = await Llm.AskAsync<AzureOpenAIChatResponse>(request, cancelToken);

        return LastResponse.Text ?? string.Empty;
    }

    /// <summary>
    /// Recursive search of files system for supported documents. Uploads documents to Azure Search.
    /// </summary>
    /// <param name="cancelToken"><see cref="CancellationToken"/></param>
    public override async Task ProcessFilesAsync(CancellationToken cancelToken)
    {
        IEnumerable<string> docExtensions = DocumentTypeFactory.GetSupportedDocumentTypes();

        foreach (FileInfo doc in DocSource.GetFiles(cancelToken))
        {
            ArgumentNullException.ThrowIfNull(doc);
            string filePath = doc.FullName;
            ArgumentException.ThrowIfNullOrEmpty(filePath);
            string extension = Path.GetExtension(filePath);

            if (string.IsNullOrWhiteSpace(extension))
                extension = "none";

            if (docExtensions.Contains(extension))
            {
                string fileRef = Path.GetFileName(filePath);

                IDocumentType docType = DocumentTypeFactory.Create(fileRef);
                Stream fileStream = File.OpenRead(filePath);

                List<TextChunk> chunks = await Rag.EmbedAsync(fileStream, docType, filePath, cancelToken);
                await Rag.IndexAsync(chunks, cancelToken);

                Console.WriteLine($"Document {fileRef} is of type {docType.GetType().Name}");
            }
            else
            {
                Console.WriteLine($"Document {doc} is not supported");
            }

            if (cancelToken.IsCancellationRequested)
                break;
        }
    }
}

using System.Security.Cryptography;
using System.Text;

using Azure.AI.OpenAI;

using Generellem.Document.DocumentTypes;
using Generellem.DocumentSource;
using Generellem.Llm;
using Generellem.Llm.AzureOpenAI;
using Generellem.Rag;
using Generellem.Repository;
using Generellem.Services;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Generellem.Orchestrator;

/// <summary>
/// Orchestrates Retrieval-Augmented Generation (RAG)
/// </summary>
/// <remarks>
/// Inspired by Retrieval-Augmented Generation (RAG)/Bea Stollnitz at https://bea.stollnitz.com/blog/rag/
/// </remarks>
public class AzureOpenAIOrchestrator(
    IConfiguration config, 
    IDocumentHashRepository docHashRep,
    IDocumentSourceFactory docSourceFact, 
    ILlm llm,
    ILogger<AzureOpenAIOrchestrator> logger,
    IRag rag) 
    : GenerellemOrchestratorBase(docSourceFact, llm, rag)
{
    readonly IConfiguration config = config;

    /// <summary>
    /// We use this to summarize the user query, based on recent context.
    /// </summary>
    protected string ContextMessage { get; set; } =
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

        List<string> matchingDocuments = await Rag.SearchAsync(userIntent, cancelToken);

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

        ChatCompletionsOptions chatCompletionOptions = new(deploymentName, messages);
        AzureOpenAIChatRequest request = new(chatCompletionOptions);

        AzureOpenAIChatResponse lastResponse = await Llm.AskAsync<AzureOpenAIChatResponse>(request, cancelToken);

        ManageChatHistory(chatHistory, userQuery);

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

        AzureOpenAIChatResponse lastResponse = await Llm.AskAsync<AzureOpenAIChatResponse>(request, cancelToken);

        return lastResponse.Text ?? string.Empty;
    }

    /// <summary>
    /// Recursive search of files system for supported documents. Uploads documents to Azure Search.
    /// </summary>
    /// <param name="cancelToken"><see cref="CancellationToken"/></param>
    public override async Task ProcessFilesAsync(CancellationToken cancelToken)
    {
        logger.LogInformation(GenerellemLogEvents.Information, $"Processing document sources...");

        foreach (IDocumentSource docSource in DocSources)
        {
            List<string> fileRefs = new();

            await foreach (DocumentInfo doc in docSource.GetDocumentsAsync(cancelToken))
            {
                ArgumentNullException.ThrowIfNull(doc);
                ArgumentNullException.ThrowIfNull(doc.DocStream);
                ArgumentNullException.ThrowIfNull(doc.DocType);
                ArgumentException.ThrowIfNullOrEmpty(doc.FilePath);
                ArgumentException.ThrowIfNullOrEmpty(doc.FileRef);

                if (doc.DocType.GetType() == typeof(Unknown))
                    continue;

                fileRefs.Add(doc.FileRef);

                string fullText = await doc.DocType.GetTextAsync(doc.DocStream, doc.FilePath);

                if (IsDocUnchanged(doc, fullText))
                    continue;

                logger.LogInformation(GenerellemLogEvents.Information, "Ingesting {FileRef}", doc.FileRef);

                List<TextChunk> chunks = await Rag.EmbedAsync(fullText, doc.DocType, doc.FileRef, cancelToken);
                await Rag.IndexAsync(chunks, cancelToken);

                if (cancelToken.IsCancellationRequested)
                    break;
            }

            await Rag.RemoveDeletedFilesAsync(docSource.Prefix, fileRefs, cancelToken);
        }
    }

    /// <summary>
    /// Compares hash of new document vs. hash of previous document to determine if anything changed.
    /// </summary>
    /// <remarks>
    /// This is an optimization to ensure we don't update documents that haven't changed.
    /// If the document doesn't exist in the local DB, it's new and we insert it.
    /// If the hashes are different, we insert the document into the local DB.
    /// </remarks>
    /// <param name="doc"><see cref="DocumentInfo"/> metadata of document.</param>
    /// <param name="fullText">Document text.</param>
    /// <returns>True if the current and previous hashes match.</returns>
    protected virtual bool IsDocUnchanged(DocumentInfo doc, string fullText)
    {
        string newHash = ComputeSha256Hash(fullText);

        DocumentHash? document = docHashRep.GetDocumentHash(doc.FileRef);

        if (document == null)
            docHashRep.Insert(new DocumentHash { FileRef = doc.FileRef, Hash = newHash });
        else if (document.Hash != newHash)
            docHashRep.Update(document, newHash);
        else
            return true;

        return false;
    }

    /// <summary>
    /// We're using a SHA256 hash to compare documents.
    /// </summary>
    /// <param name="rawData">Document Text.</param>
    /// <returns>Hash of the document text.</returns>
    protected static string ComputeSha256Hash(string rawData)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawData));

        StringBuilder sb = new();

        for (int i = 0; i < bytes.Length; i++)
            sb.Append(bytes[i].ToString("x2"));

        return sb.ToString();
    }
}

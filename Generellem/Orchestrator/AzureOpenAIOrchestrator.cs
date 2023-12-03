using Azure.AI.OpenAI;

using Generellem.DataSource;
using Generellem.Document;
using Generellem.Document.DocumentTypes;
using Generellem.Llm;
using Generellem.Llm.AzureOpenAI;
using Generellem.Rag;

namespace Generellem.Orchestrator;

/// <summary>
/// Orchestrates Retrieval-Augmented Generation (RAG)
/// </summary>
/// <remarks>
/// Inspired byRetrieval-Augmented Generation (RAG)/Bea Stollnitz at https://bea.stollnitz.com/blog/rag/
/// </remarks>
public class AzureOpenAIOrchestrator : GenerellemOrchestrator
{
    public AzureOpenAIOrchestrator(IDocumentSource docSource, ILlm llm, IRag rag)
        : base(docSource, llm, rag)
    {
    }

    public AzureOpenAIChatResponse? LastResponse { get; set; }

    public string? SystemMessage { get; set; } =
        "You are a professional AI bot that returns accurate content for busy workers.\n" +
        "Please answer the user's question using only information you can find in the context.\n" +
        "If the user's question is unrelated to the information in the context, say you don't know.\n";

    /// <summary>
    /// Searches for context, builds a prompt, and gets a response from Azure OpenAI
    /// </summary>
    /// <param name="requestText">User's request</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    /// <returns>Azure OpenAI response</returns>
    /// <exception cref="ArgumentNullException">Throws if config values not found</exception>
    public override async Task<string> AskAsync(string requestText, CancellationToken cancellationToken)
    {
        string? deploymentName = Environment.GetEnvironmentVariable("OPENAI_DEPLOYMENT_NAME");
        _ = deploymentName ?? throw new ArgumentNullException(nameof(deploymentName));

        List<string> searchResponse = await Rag.SearchAsync(requestText, cancellationToken);

        string context = 
            "Context: \n\n" +
            "```" +
            string.Join("\n\n", searchResponse) +
            "```\n";

        List<ChatMessage> messages = new()
        {
            new ChatMessage(ChatRole.System, SystemMessage + context),
            new ChatMessage(ChatRole.User, requestText)
        };

        ChatCompletionsOptions chatCompletionOptions = new(deploymentName, messages);
        AzureOpenAIChatRequest request = new(chatCompletionOptions);

        LastResponse = await Llm.AskAsync<AzureOpenAIChatResponse>(request, cancellationToken);

        return LastResponse.Text ?? string.Empty;
    }

    /// <summary>
    /// Recursive search of files system for supported documents. Uploads documents to Azure Search.
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/></param>
    public override async Task ProcessFilesAsync(CancellationToken cancellationToken)
    {
        IEnumerable<string> docExtensions = DocumentTypeFactory.GetSupportedDocumentTypes();

        foreach (FileInfo doc in DocSource.GetFiles(cancellationToken))
        {
            ArgumentNullException.ThrowIfNull(doc);
            string path = doc.FullName;
            ArgumentException.ThrowIfNullOrEmpty(path);
            string extension = Path.GetExtension(path);

            if (string.IsNullOrWhiteSpace(extension))
                extension = "none";

            if (docExtensions.Contains(extension))
            {
                string fileRef = Path.GetFileName(path);

                IDocumentType docType = DocumentTypeFactory.Create(fileRef);
                Stream fileStream = File.OpenRead(path);

                List<TextChunk> chunks = await Rag.EmbedAsync(fileStream, docType, fileRef, cancellationToken);
                await Rag.IndexAsync(chunks, cancellationToken);

                Console.WriteLine($"Document {path} is of type {docType.GetType().Name}");
            }
            else
            {
                Console.WriteLine($"Document {doc} is not supported");
            }
        }
    }
}

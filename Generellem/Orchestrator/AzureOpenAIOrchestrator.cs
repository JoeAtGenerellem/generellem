using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

using Azure.AI.OpenAI;

using Generellem.DataSource;
using Generellem.Document;
using Generellem.Llm;
using Generellem.Llm.AzureOpenAI;
using Generellem.Rag;
using Generellem.Services;

namespace Generellem.Orchestrator;

public class AzureOpenAIOrchestrator : GenerellemOrchestrator
{
    readonly IAzureBlobService blobService;
    readonly IAzureSearchService searchService;

    public AzureOpenAIOrchestrator(
        IAzureBlobService blobService,
        IAzureSearchService searchService,
        IDocumentSource docSource, 
        ILlm llm, 
        IRag rag)
        : base(docSource, llm, rag)
    {
        this.blobService = blobService;
        this.searchService = searchService;
    }

    public AzureOpenAIChatResponse? LastResponse { get; set; }

    public string? SystemMessage { get; set; } = "You are a professional AI bot that returns accurate content for busy workers.";

    public override async Task<string> AskAsync(string message, CancellationToken cancellationToken)
    {
        string? deploymentName = Environment.GetEnvironmentVariable("OPENAI_DEPLOYMENT_NAME");
        _ = deploymentName ?? throw new ArgumentNullException(nameof(deploymentName));

        List<ChatMessage> messages = new()
        {
            new ChatMessage(ChatRole.System, SystemMessage),
            new ChatMessage(ChatRole.User, message)
        };

        List<string> searchResponse = await Rag.SearchAsync(message, cancellationToken);
        messages.AddRange(
            (from response in searchResponse
             select new ChatMessage(ChatRole.Tool, response))
            .ToList());

        ChatCompletionsOptions chatCompletionOptions = new(deploymentName, messages);
        AzureOpenAIChatRequest request = new(chatCompletionOptions);

        LastResponse = await Llm.AskAsync<AzureOpenAIChatResponse>(request, cancellationToken);

        return LastResponse.Text ?? string.Empty;
    }

    string GetHashedPathAsFileName(string path)
    {
        string extension = Path.GetExtension(path);
        ArgumentException.ThrowIfNullOrEmpty(extension);

        using var md5 = MD5.Create();
        byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(path));

        StringBuilder sb = new StringBuilder();

        foreach (byte b in hash)
            sb.Append(b.ToString("X2"));

        return sb.ToString() + extension;
    }

    public override async Task ProcessFilesAsync(CancellationToken cancellationToken)
    {
        IEnumerable<string> docExtensions = DocumentTypeFactory.GetSupportedDocumentTypes();

        foreach (var doc in DocSource.GetFiles(cancellationToken))
        {
            ArgumentNullException.ThrowIfNull(doc);
            string path = doc.FullName;
            ArgumentException.ThrowIfNullOrEmpty(path);
            string extension = Path.GetExtension(path);
            ArgumentException.ThrowIfNullOrEmpty(extension);

            if (docExtensions.Contains(extension))
            {
                string fileName = GetHashedPathAsFileName(path);
                await blobService.UploadAsync(fileName, File.OpenRead(path));
                await searchService.RunIndexerAsync();

                var document = DocumentTypeFactory.Create(fileName);

                Console.WriteLine($"Document {path} is of type {document.GetType().Name}");
            }
            else
            {
                Console.WriteLine($"Document {doc} is not supported");
            }
        }
    }
}

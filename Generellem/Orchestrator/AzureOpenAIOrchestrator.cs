using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using Azure;
using Azure.AI.OpenAI;

using Generellem.DataSource;
using Generellem.Document;
using Generellem.Document.DocumentTypes;
using Generellem.Llm;
using Generellem.RAG;
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

    public override async Task<string> AskAsync(string message, CancellationToken cancellationToken)
    {
        string? endpoint = Environment.GetEnvironmentVariable("OPENAI_ENDPOINT_NAME");
        _ = endpoint ?? throw new ArgumentNullException(nameof(endpoint));

        string? key = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        _ = key ?? throw new ArgumentNullException(nameof(key));

        string? deploymentName = Environment.GetEnvironmentVariable("OPENAI_DEPLOYMENT_NAME");
        _ = deploymentName ?? throw new ArgumentNullException(nameof(deploymentName));

        OpenAIClient client = new(new Uri(endpoint), new AzureKeyCredential(key));

        List<ChatMessage> messages = new()
        {
            new ChatMessage(ChatRole.System, "You are a professional AI bot that returns accurate content for busy workers."),
            new ChatMessage(ChatRole.User, message)
        };

        ChatCompletionsOptions chatCompletionOptions = new(deploymentName, messages);

        ChatCompletions chatCompletionsResponse = client.GetChatCompletions(chatCompletionOptions, cancellationToken);

        ChatMessage completion = chatCompletionsResponse.Choices[0].Message;

        return await Task.FromResult(completion.Content);
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
                //await blobService.DeleteAsync(fileName);
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

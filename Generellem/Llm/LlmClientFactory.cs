using Azure.AI.OpenAI;

using Generellem.Services;

using OpenAI;
using OpenAI.Chat;
using OpenAI.Embeddings;

using System.ClientModel;

namespace Generellem.Llm;

public class LlmClientFactory(IDynamicConfiguration config) : ILlmClientFactory
{
    public virtual AzureOpenAIClient CreateOpenAIClient()
    {
        string? endpoint = config[GKeys.AzOpenAIEndpointName];
        ArgumentNullException.ThrowIfNull(endpoint, nameof(endpoint));

        string? key = config[GKeys.AzOpenAIApiKey];
        ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));

        AzureOpenAIClient client = new(new Uri(endpoint), new ApiKeyCredential(key));

        return client;
    }

    public virtual ChatClient CreateChatClient()
    {
        OpenAIClient client = CreateOpenAIClient();
        ChatClient chatClient = client.GetChatClient(config[GKeys.AzOpenAIDeploymentName]);

        return chatClient;
    }

    public virtual EmbeddingClient CreateEmbeddingClient()
    {
        OpenAIClient client = CreateOpenAIClient();
        EmbeddingClient embeddingClient = client.GetEmbeddingClient(config[GKeys.AzOpenAIEmbeddingName]);

        return embeddingClient;
    }
}

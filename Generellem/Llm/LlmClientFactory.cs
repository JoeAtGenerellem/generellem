using Azure;
using Azure.AI.OpenAI;

using Generellem.Services;

namespace Generellem.Llm;

public class LlmClientFactory(IDynamicConfiguration config) : ILlmClientFactory
{
    public virtual OpenAIClient CreateOpenAIClient()
    {
        string? endpointName = config[GKeys.AzOpenAIEndpointName];
        ArgumentException.ThrowIfNullOrWhiteSpace(endpointName, nameof(endpointName));

        string? key = config[GKeys.AzOpenAIApiKey]; ;
        ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));

        OpenAIClient client = new(new Uri(endpointName), new AzureKeyCredential(key));

        return client;
    }
}

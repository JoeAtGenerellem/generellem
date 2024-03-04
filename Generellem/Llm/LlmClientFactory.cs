using Azure;
using Azure.AI.OpenAI;

using Generellem.Services;

namespace Generellem.Llm;

public class LlmClientFactory
{
    readonly IGenerellemConfiguration config;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    /// <summary>
    /// This constructor only supports unit testing - don't use for anything else.
    /// </summary>
    protected LlmClientFactory() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public LlmClientFactory(IGenerellemConfiguration config)
    {
        this.config = config;
    }

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

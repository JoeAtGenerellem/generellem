using Azure;
using Azure.AI.OpenAI;

using Generellem.Services;

using Microsoft.Extensions.Configuration;
namespace Generellem.Llm.AzureOpenAI;

public class AzureOpenAILlm : ILlm
{
    readonly IConfiguration config;

    public AzureOpenAILlm(IConfiguration config)
    {
        this.config = config;
    }

    public virtual async Task<TResponse> AskAsync<TResponse>(IChatRequest request, CancellationToken cancellationToken)
        where TResponse : IChatResponse
    {
        string? endpoint = config[GKeys.AzOpenAIEndpointName];
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint, nameof(endpoint));

        string? key = config[GKeys.AzOpenAIApiKey];
        ArgumentException.ThrowIfNullOrWhiteSpace(key, nameof(key));

        ChatCompletionsOptions? completionsOptions = (request as AzureOpenAIChatRequest)?.Options;
        ArgumentNullException.ThrowIfNull(completionsOptions, nameof(completionsOptions));

        OpenAIClient client = new(new Uri(endpoint), new AzureKeyCredential(key));

        ChatCompletions chatCompletionsResponse = await client.GetChatCompletionsAsync(completionsOptions, cancellationToken);

        IChatResponse chatResponse = new AzureOpenAIChatResponse(chatCompletionsResponse);

        return (TResponse)chatResponse;
    }
}

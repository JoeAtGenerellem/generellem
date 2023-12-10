using Azure;
using Azure.AI.OpenAI;

using Generellem.Services;

using Microsoft.Extensions.Configuration;
namespace Generellem.Llm.AzureOpenAI;

public class AzureOpenAILlm : ILlm
{
    readonly IConfiguration config;

    readonly OpenAIClient openAIClient;

    public AzureOpenAILlm(IConfiguration config, LlmClientFactory llmClientFact)
    {
        this.config = config;
        this.openAIClient = llmClientFact.CreateOpenAIClient();
    }

    public virtual async Task<TResponse> AskAsync<TResponse>(IChatRequest request, CancellationToken cancellationToken)
        where TResponse : IChatResponse
    {
        ChatCompletionsOptions? completionsOptions = (request as AzureOpenAIChatRequest)?.Options;
        ArgumentNullException.ThrowIfNull(completionsOptions, nameof(completionsOptions));

        ChatCompletions chatCompletionsResponse = await openAIClient.GetChatCompletionsAsync(completionsOptions, cancellationToken);

        IChatResponse chatResponse = new AzureOpenAIChatResponse(chatCompletionsResponse);

        return (TResponse)chatResponse;
    }
}

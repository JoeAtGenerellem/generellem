using System.Threading;

using Azure;
using Azure.AI.OpenAI;

using Generellem.Services;
using Microsoft.Extensions.Configuration;

using Polly;
using Polly.Retry;
namespace Generellem.Llm.AzureOpenAI;

public class AzureOpenAILlm : ILlm
{
    readonly IConfiguration config;

    readonly OpenAIClient openAIClient;
    readonly ResiliencePipeline pipeline;

    public AzureOpenAILlm(IConfiguration config, LlmClientFactory llmClientFact)
    {
        this.config = config;
        this.openAIClient = llmClientFact.CreateOpenAIClient();

        pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions())
            .AddTimeout(TimeSpan.FromSeconds(3))
            .Build();
    }

    public virtual async Task<TResponse> AskAsync<TResponse>(IChatRequest request, CancellationToken cancellationToken)
        where TResponse : IChatResponse
    {
        ChatCompletionsOptions? completionsOptions = (request as AzureOpenAIChatRequest)?.Options;
        ArgumentNullException.ThrowIfNull(completionsOptions, nameof(completionsOptions));

        ChatCompletions chatCompletionsResponse = await pipeline.ExecuteAsync< ChatCompletions>(
            async token => await openAIClient.GetChatCompletionsAsync(completionsOptions, token), 
            cancellationToken);

        IChatResponse chatResponse = new AzureOpenAIChatResponse(chatCompletionsResponse);

        return (TResponse)chatResponse;
    }
}

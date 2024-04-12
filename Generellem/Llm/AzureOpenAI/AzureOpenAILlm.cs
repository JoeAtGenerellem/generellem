using Azure;
using Azure.AI.OpenAI;

using Generellem.Services;

using Microsoft.Extensions.Logging;

using Polly;
using Polly.Retry;

namespace Generellem.Llm.AzureOpenAI;

public class AzureOpenAILlm(LlmClientFactory llmClientFact, ILogger<AzureOpenAILlm> logger) : ILlm
{
    readonly ILogger<AzureOpenAILlm> logger = logger;

    readonly OpenAIClient openAIClient = llmClientFact.CreateOpenAIClient();

    readonly ResiliencePipeline pipeline = 
        new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions())
            .AddTimeout(TimeSpan.FromSeconds(7))
            .Build();

    public virtual async Task<TResponse> PromptAsync<TResponse>(IChatRequest? request, CancellationToken cancellationToken)
        where TResponse : IChatResponse
    {
        ChatCompletionsOptions? completionsOptions = (request as AzureOpenAIChatRequest)?.Options;
        ArgumentNullException.ThrowIfNull(completionsOptions, nameof(completionsOptions));

        try
        {
            ChatCompletions chatCompletionsResponse = await pipeline.ExecuteAsync<ChatCompletions>(
                async token => await openAIClient.GetChatCompletionsAsync(completionsOptions, token),
                cancellationToken);

            IChatResponse chatResponse = new AzureOpenAIChatResponse(chatCompletionsResponse);

            return (TResponse)chatResponse;
        }
        catch (RequestFailedException rfEx)
        {
            logger.LogError(GenerellemLogEvents.AuthorizationFailure, rfEx, "Please check credentials and exception details for more info.");
            throw;
        }
    }
}

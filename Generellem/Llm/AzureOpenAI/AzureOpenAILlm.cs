using Azure;
using Azure.AI.OpenAI;

using Generellem.Security;
namespace Generellem.Llm.AzureOpenAI;

public class AzureOpenAILlm : ILlm
{
    readonly ISecretStore secretStore;

    public AzureOpenAILlm(ISecretStore secretStore)
    {
        this.secretStore = secretStore;
    }

    public async Task<TResponse> AskAsync<TResponse>(IChatRequest request, CancellationToken cancellationToken)
        where TResponse : IChatResponse
    {
        string? endpoint = Environment.GetEnvironmentVariable("OPENAI_ENDPOINT_NAME");
        _ = endpoint ?? throw new ArgumentNullException(nameof(endpoint));

        string? key = secretStore["OPENAI_API_KEY"];
        _ = key ?? throw new ArgumentNullException(nameof(key));

        ChatCompletionsOptions? completionsOptions = (request as AzureOpenAIChatRequest)?.Options;

        if (completionsOptions == null)
            throw new ArgumentNullException(nameof(completionsOptions));

        OpenAIClient client = new(new Uri(endpoint), new AzureKeyCredential(key));

        ChatCompletions chatCompletionsResponse = await client.GetChatCompletionsAsync(completionsOptions, cancellationToken);

        IChatResponse chatResponse = new AzureOpenAIChatResponse(chatCompletionsResponse);

        return (TResponse)chatResponse;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Azure;
using Azure.AI.OpenAI;
namespace Generellem.Llm.AzureOpenAI;

public class AzureOpenAILlm : ILlm
{
    public async Task<TResponse> AskAsync<TResponse>(IChatRequest request, CancellationToken cancellationToken)
        where TResponse : IChatResponse
    {
        string? endpoint = Environment.GetEnvironmentVariable("OPENAI_ENDPOINT_NAME");
        _ = endpoint ?? throw new ArgumentNullException(nameof(endpoint));

        string? key = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
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

using Azure.AI.OpenAI;

namespace Generellem.Llm;

public class AzureOpenAIChatResponse : IChatResponse
{
    public AzureOpenAIChatResponse(ChatCompletions chatCompletionsResponse)
    {
        this.ChatCompletionsResponse = chatCompletionsResponse;
    }

    public virtual ChatCompletions ChatCompletionsResponse { get; init; }

    public virtual string? Text => ChatCompletionsResponse?.Choices[0]?.Message?.Content;
}

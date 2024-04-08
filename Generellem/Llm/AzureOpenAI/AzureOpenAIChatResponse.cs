using Azure.AI.OpenAI;

namespace Generellem.Llm;

public class AzureOpenAIChatResponse : IChatResponse
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    /// <summary>
    /// This constructor only supports unit testing - don't use for anything else.
    /// </summary>
    public AzureOpenAIChatResponse()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    {
    }

    public AzureOpenAIChatResponse(ChatCompletions chatCompletionsResponse)
    {
        this.ChatCompletionsResponse = chatCompletionsResponse;
    }

    public virtual ChatCompletions ChatCompletionsResponse { get; init; }

    public virtual string? Text => ChatCompletionsResponse?.Choices[0]?.Message?.Content;
}

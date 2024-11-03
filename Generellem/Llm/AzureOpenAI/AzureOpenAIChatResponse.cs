using OpenAI.Chat;

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

    public AzureOpenAIChatResponse(ChatCompletion chatCompletionsResponse)
    {
        this.ChatCompletionsResponse = chatCompletionsResponse;
    }

    public virtual ChatCompletion ChatCompletionsResponse { get; init; }

    public virtual string Text
    {
        get         
        {
            if (ChatCompletionsResponse is not null && ChatCompletionsResponse.Content is not null && ChatCompletionsResponse.Content.Count > 0)
                return ChatCompletionsResponse.Content[0].Text;
            else
                return string.Empty;
        }
    }
}

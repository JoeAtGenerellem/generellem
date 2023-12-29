using Azure.AI.OpenAI;

namespace Generellem.Llm.AzureOpenAI;

public class AzureOpenAIChatRequest(ChatCompletionsOptions? options) : IChatRequest
{
    public ChatCompletionsOptions? Options { get; set; } = options;

    public virtual string Text 
    {
        get
        {
            return Options?.Messages[0]?.Content ?? string.Empty;
        }
        set
        {
            string? text = Options?.Messages[0]?.Content;

            if (text is not null)
                Options!.Messages[0].Content = value;
        }
    }
}

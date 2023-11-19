using Azure.AI.OpenAI;

namespace Generellem.Llm.AzureOpenAI;

public class AzureOpenAIChatRequest : IChatRequest
{
    public ChatCompletionsOptions Options { get; set; }

    public AzureOpenAIChatRequest(ChatCompletionsOptions options)
    {
        this.Options = options;
    }

    public string Text 
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

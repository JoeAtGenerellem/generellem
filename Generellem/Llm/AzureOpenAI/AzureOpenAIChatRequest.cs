using Azure.AI.OpenAI;

using Generellem.Embedding;

namespace Generellem.Llm.AzureOpenAI;

public class AzureOpenAIChatRequest : IChatRequest
{
    public Queue<ChatMessage> ChatHistory { get; set; } = new();

    public ChatCompletionsOptions Options { get; set; } = new();

    public string SummarizedUserIntent { get; set; } = string.Empty;

    public List<TextChunk> TextChunks { get; set; } = new();

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

using Azure.AI.OpenAI;

using Generellem.Embedding;
using Generellem.Processors;

namespace Generellem.Llm.AzureOpenAI;

public class AzureOpenAIChatRequest : IChatRequest
{
    public Queue<ChatMessage> ChatHistory { get; set; } = new();

    public ChatCompletionsOptions Options { get; set; } = new();

    public QueryDetails<AzureOpenAIChatRequest, AzureOpenAIChatResponse> SummarizedUserIntent { get; set; } = new();

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

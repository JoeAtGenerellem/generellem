using Azure.AI.OpenAI;

using Generellem.Embedding;
using Generellem.Processors;

namespace Generellem.Llm.AzureOpenAI;

public class AzureOpenAIChatRequest : IChatRequest
{
    public ChatCompletionsOptions Options { get; set; } = new();

    public QueryDetails<AzureOpenAIChatRequest, AzureOpenAIChatResponse> SummarizedUserIntent { get; set; } = new();

    public List<TextChunk> TextChunks { get; set; } = new();

    public virtual string Text 
    {
        get
        {
            return (Options?.Messages[0] as ChatRequestSystemMessage)?.Content ?? string.Empty;
        }
        set
        {
            string? text = (Options?.Messages[0] as ChatRequestSystemMessage)?.Content;

            if (text is not null)
                Options!.Messages[0] = new ChatRequestSystemMessage(value);
        }
    }

    public static string GetRequestContent(ChatRequestMessage chatMessage) =>
        chatMessage switch
        {
            ChatRequestUserMessage userMessage => userMessage.Content,
            ChatRequestAssistantMessage assistantMessage => assistantMessage.Content,
            _ => string.Empty
        };

}

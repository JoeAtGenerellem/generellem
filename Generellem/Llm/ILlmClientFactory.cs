using Azure.AI.OpenAI;

using OpenAI.Chat;
using OpenAI.Embeddings;

namespace Generellem.Llm;

public interface ILlmClientFactory
{
    ChatClient CreateChatClient();
    EmbeddingClient CreateEmbeddingClient();
    AzureOpenAIClient CreateOpenAIClient();
}
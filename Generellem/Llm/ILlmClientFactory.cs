using Azure.AI.OpenAI;

using OpenAI.Chat;

namespace Generellem.Llm;

public interface ILlmClientFactory
{
    ChatClient CreateChatClient();
    AzureOpenAIClient CreateOpenAIClient();
}
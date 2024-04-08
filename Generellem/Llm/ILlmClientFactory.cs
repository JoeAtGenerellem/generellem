using Azure.AI.OpenAI;

namespace Generellem.Llm;
public interface ILlmClientFactory
{
    OpenAIClient CreateOpenAIClient();
}
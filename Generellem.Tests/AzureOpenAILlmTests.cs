using Azure;
using Azure.AI.OpenAI;

using Generellem.Llm.AzureOpenAI;
using Generellem.Services;

using Microsoft.Extensions.Configuration;

namespace Generellem.Llm.Tests;

public class AzureOpenAILlmTests
{
    readonly Mock<IConfiguration> configMock = new();
    readonly Mock<LlmClientFactory> llmClientFactMock = new();
    readonly Mock<OpenAIClient> openAIClientMock = new();
    readonly Mock<Response<ChatCompletions>> completionsMock = new();

    readonly AzureOpenAILlm llm;

    public AzureOpenAILlmTests()
    {
        configMock
            .Setup(config => config[GKeys.AzOpenAIEndpointName])
            .Returns("https://generellem");
        configMock
            .Setup(config => config[GKeys.AzOpenAIApiKey])
            .Returns("generellem-key");

        List<ChatChoice> chatChoices =
        [
            AzureOpenAIModelFactory.ChatChoice(
                new ChatMessage(ChatRole.Assistant, "Generellem lets users use their own data for AI."))
        ];
        ChatCompletions completions = AzureOpenAIModelFactory.ChatCompletions(
            Guid.NewGuid().ToString(),
            DateTimeOffset.Now,
            chatChoices);
        openAIClientMock.Setup(client => client.GetChatCompletionsAsync(It.IsAny<ChatCompletionsOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(completionsMock.Object);
        completionsMock.SetupGet(m => m.Value).Returns(completions);
        llmClientFactMock.Setup(m => m.CreateOpenAIClient()).Returns(openAIClientMock.Object);

        llm = new AzureOpenAILlm(configMock.Object, llmClientFactMock.Object);
    }
    [Fact]
    public void TestAskAsync_WithNullEndpoint_ThrowsArgumentException()
    {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        Assert.ThrowsAsync<ArgumentException>(() =>
            llm.AskAsync<IChatResponse>(null, CancellationToken.None));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
    }

    [Fact]
    public async Task TestAskAsync_WithValidInput_ReturnsResponse()
    {
        List<ChatMessage> chatMessages =
        [
            new ChatMessage(ChatRole.User, "What is Generellem?")
        ];
        var request = new AzureOpenAIChatRequest(new ChatCompletionsOptions("generellem-deployment", chatMessages));

        var result = await llm.AskAsync<IChatResponse>(request, CancellationToken.None);

        Assert.IsType<AzureOpenAIChatResponse>(result);
    }

    [Fact]
    public void TestAskAsync_WithNullCompletionsOptions_ThrowsArgumentNullException()
    {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        var request = new AzureOpenAIChatRequest(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        Assert.ThrowsAsync<ArgumentNullException>(() =>
            llm.AskAsync<IChatResponse>(request, CancellationToken.None));
    }
}

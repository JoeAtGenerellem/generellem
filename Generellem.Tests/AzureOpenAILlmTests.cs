using Azure;
using Azure.AI.OpenAI;

using Generellem.Llm.AzureOpenAI;
using Generellem.Services;

using Microsoft.Extensions.Logging;

namespace Generellem.Llm.Tests;

public class AzureOpenAILlmTests
{
    readonly Mock<IDynamicConfiguration> configMock = new();
    readonly Mock<ILogger<AzureOpenAILlm>> logMock = new();
    readonly Mock<OpenAIClient> openAIClientMock = new();
    readonly Mock<Response<ChatCompletions>> completionsMock = new();

    readonly Mock<LlmClientFactory> llmClientFactMock;

    readonly AzureOpenAILlm llm;

    public AzureOpenAILlmTests()
    {
        configMock
            .Setup(config => config[GKeys.AzOpenAIEndpointName])
            .Returns("https://generellem");
        configMock
            .Setup(config => config[GKeys.AzOpenAIApiKey])
            .Returns("generellem-key");
        llmClientFactMock = new(configMock.Object);

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

        llm = new AzureOpenAILlm(llmClientFactMock.Object, logMock.Object);
    }

    [Fact]
    public async Task PromptAsync_WithNullEndpoint_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            llm.PromptAsync<IChatResponse>(null, CancellationToken.None));
    }

    [Fact]
    public async Task PromptAsync_WithValidInput_ReturnsResponse()
    {
        List<ChatMessage> chatMessages =
        [
            new ChatMessage(ChatRole.User, "What is Generellem?")
        ];
        var request = new AzureOpenAIChatRequest()
        {
            Options = new ChatCompletionsOptions("generellem-deployment", chatMessages)
        };

        var result = await llm.PromptAsync<IChatResponse>(request, CancellationToken.None);

        Assert.IsType<AzureOpenAIChatResponse>(result);
    }

    [Fact]
    public async Task PromptAsync_WithRequestFailedExceptionOnGetChatCompletions_LogsAnError()
    {
        AzureOpenAIChatRequest request = new()
        {
            Options = new ChatCompletionsOptions("mydeployment", [new ChatMessage()])
        };

        openAIClientMock
            .Setup(client => client.GetChatCompletionsAsync(It.IsAny<ChatCompletionsOptions>(), It.IsAny<CancellationToken>()))
            .Throws(new RequestFailedException("Unauthorized"));

        await Assert.ThrowsAsync<RequestFailedException>(async () =>
            await llm.PromptAsync<IChatResponse>(request, CancellationToken.None));

        logMock
            .Verify(
                l => l.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
                Times.Once);
    }
}

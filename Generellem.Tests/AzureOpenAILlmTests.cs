using Azure;
using Azure.AI.OpenAI;

using Generellem.Llm.AzureOpenAI;

using Microsoft.Extensions.Logging;

namespace Generellem.Llm.Tests;

public class AzureOpenAILlmTests
{
    readonly Mock<ILogger<AzureOpenAILlm>> logMock = new();
    readonly Mock<LlmClientFactory> llmClientFactMock = new();
    readonly Mock<OpenAIClient> openAIClientMock = new();
    readonly Mock<Response<ChatCompletions>> completionsMock = new();

    readonly AzureOpenAILlm llm;

    public AzureOpenAILlmTests()
    {
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
    public async Task TestAskAsync_WithNullEndpoint_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            llm.AskAsync<IChatResponse>(null, CancellationToken.None));
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
    public async Task TestAskAsync_WithNullCompletionsOptions_ThrowsArgumentNullException()
    {
        var request = new AzureOpenAIChatRequest(null);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            llm.AskAsync<IChatResponse>(request, CancellationToken.None));
    }

    [Fact]
    public async Task AskAsync_WithRequestFailedExceptionOnGetChatCompletions_LogsAnError()
    {
        AzureOpenAIChatRequest request = new(new ChatCompletionsOptions("mydeployment", [new ChatMessage()]));

        openAIClientMock
            .Setup(client => client.GetChatCompletionsAsync(It.IsAny<ChatCompletionsOptions>(), It.IsAny<CancellationToken>()))
            .Throws(new RequestFailedException("Unauthorized"));

        await Assert.ThrowsAsync<RequestFailedException>(async () =>
            await llm.AskAsync<IChatResponse>(request, CancellationToken.None));

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

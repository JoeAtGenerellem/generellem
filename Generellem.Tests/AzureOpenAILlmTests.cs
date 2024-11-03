using Azure;

using Generellem.Llm.AzureOpenAI;
using Generellem.Services;
using Generellem.Tests;

using Microsoft.Extensions.Logging;

using OpenAI;
using OpenAI.Chat;

using Polly;

namespace Generellem.Llm.Tests;

public class AzureOpenAILlmTests
{
    readonly Mock<IDynamicConfiguration> configMock = new();
    readonly Mock<ILogger<AzureOpenAILlm>> logMock = new();
    readonly Mock<OpenAIClient> openAIClientMock = new();
    readonly Mock<ChatClient> chatClientMock = new();
    readonly Mock<ClientResultMock<ChatCompletion>> completionResultMock = new();

    readonly Mock<LlmClientFactory> llmClientFactMock;

    readonly AzureOpenAILlm llm;

    public AzureOpenAILlmTests()
    {
        openAIClientMock
            .Setup(client => client.GetChatClient(It.IsAny<string>()))
            .Returns(chatClientMock.Object);
        chatClientMock
            .Setup(client => client.CompleteChatAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatCompletionOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(completionResultMock.Object);

        configMock
            .Setup(config => config[GKeys.AzOpenAIEndpointName])
            .Returns("https://generellem");
        configMock
            .Setup(config => config[GKeys.AzOpenAIApiKey])
            .Returns("generellem-key");

        llmClientFactMock = new(configMock.Object);
        llmClientFactMock
            .Setup(m => m.CreateChatClient())
            .Returns(chatClientMock.Object);

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
        AzureOpenAIChatRequest request = new()
        {
            Messages =
            [
                new UserChatMessage("What is Generellem?")
            ]
        };

        var result = await llm.PromptAsync<IChatResponse>(request, CancellationToken.None);

        Assert.IsType<AzureOpenAIChatResponse>(result);
    }

    [Fact]
    public async Task PromptAsync_WithRequestFailedExceptionOnGetChatCompletions_LogsAnError()
    {
        llm.Pipeline = new ResiliencePipelineBuilder().Build();
        AzureOpenAIChatRequest request = new()
        {
            Messages =
            [
                new UserChatMessage("What is Generellem?")
            ]
        };
        chatClientMock
            .Setup(client => client.CompleteChatAsync(It.IsAny<IEnumerable<ChatMessage>>(), It.IsAny<ChatCompletionOptions>(), It.IsAny<CancellationToken>()))
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

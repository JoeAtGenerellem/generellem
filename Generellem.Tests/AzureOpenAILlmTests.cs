using Azure;
using Azure.AI.OpenAI;

using Generellem.Llm.AzureOpenAI;
using Generellem.Services;

using Microsoft.Extensions.Logging;
using Polly;

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

        ChatCompletions completions = 
            AzureOpenAIModelFactory.ChatCompletions(
                Guid.NewGuid().ToString(),
                DateTimeOffset.Now,
                []);
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
        List<ChatRequestUserMessage> chatMessages =
        [
            new ChatRequestUserMessage("What is Generellem?")
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
        llm.Pipeline = new ResiliencePipelineBuilder().Build();
        AzureOpenAIChatRequest request = new()
        {
            Options = new ChatCompletionsOptions("mydeployment", [new ChatRequestUserMessage("Some Content")])
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

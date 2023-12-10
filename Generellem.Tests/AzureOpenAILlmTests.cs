using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Azure.AI.OpenAI;
using Generellem.Llm.AzureOpenAI;
using Generellem.Llm;
using Generellem.Services;
using Moq;
using Microsoft.Extensions.Configuration;
using Azure;

namespace Generellem.Tests;

public class AzureOpenAILlmTests
{
    Mock<IConfiguration> configMock = new();
    Mock<LlmClientFactory> llmClientFactMock = new();
    Mock<OpenAIClient> openAIClientMock = new();
    Mock<Response<ChatCompletions>> completionsMock = new();

    AzureOpenAILlm llm;

    public AzureOpenAILlmTests()
    {
        configMock
            .Setup(config => config[GKeys.AzOpenAIEndpointName])
            .Returns("https://generellem");
        configMock
            .Setup(config => config[GKeys.AzOpenAIApiKey])
            .Returns("generellem-key");

        List<ChatChoice> chatChoices = new()
        {
            AzureOpenAIModelFactory.ChatChoice(
                new ChatMessage(ChatRole.Assistant, "Generellem lets users use their own data for AI."))
        };
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
        Assert.ThrowsAsync<ArgumentException>(() =>
            llm.AskAsync<IChatResponse>(null, CancellationToken.None));
    }

    [Fact]
    public async Task TestAskAsync_WithValidInput_ReturnsResponse()
    {
        List<ChatMessage> chatMessages = new()
        {
            new ChatMessage(ChatRole.User, "What is Generellem?")
        };
        var request = new AzureOpenAIChatRequest(new ChatCompletionsOptions("generellem-deployment", chatMessages));

        var result = await llm.AskAsync<IChatResponse>(request, CancellationToken.None);

        Assert.IsType<AzureOpenAIChatResponse>(result);
    }

    [Fact]
    public void TestAskAsync_WithNullCompletionsOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var request = new AzureOpenAIChatRequest(null);

        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(() =>
            llm.AskAsync<IChatResponse>(request, CancellationToken.None));
    }
}

using Azure.AI.OpenAI;

using Generellem.Llm;
using Generellem.Services;

using Microsoft.Extensions.Configuration;

using Moq;

namespace Generellem.Llm.Tests;

public class LlmClientFactoryTests
{
    Mock<IConfiguration> configMock = new();

    LlmClientFactory factory;

    public LlmClientFactoryTests()
    {
        configMock
            .Setup(config => config[GKeys.AzOpenAIEndpointName])
            .Returns("https://generellem");
        configMock
            .Setup(config => config[GKeys.AzOpenAIApiKey])
            .Returns("generellem-key");

        factory = new(configMock.Object);
    }

    [Fact]
    public void CreateOpenAIClient_ReturnsOpenAIClient()
    {
        var result = factory.CreateOpenAIClient();

        Assert.IsType<OpenAIClient>(result);
    }
}

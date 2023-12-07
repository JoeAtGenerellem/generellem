using Microsoft.Extensions.Configuration;

using Moq;

namespace Generellem.Tests;
public class Setup
{
    public static Mock<IConfiguration> MockConfig()
    {
        Mock<IConfiguration> configuration = new();
        Mock<IConfigurationSection> configSection = new();

        configSection.Setup(section => section.Value).Returns("some value");
        configuration.Setup(config => config.GetSection(It.IsAny<string>())).Returns(configSection.Object);

        return configuration;
    }
}

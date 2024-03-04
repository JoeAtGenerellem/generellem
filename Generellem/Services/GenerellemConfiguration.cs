using Microsoft.Extensions.Configuration;

namespace Generellem.Services;

public class GenerellemConfiguration : IGenerellemConfiguration
{
    readonly IConfiguration config;

    readonly Dictionary<string, string> dynamicConfig = new();

    public GenerellemConfiguration(IConfiguration config)
    {
        this.config = config;
    }

    public string? this[string index]
    {
        get => dynamicConfig.ContainsKey(index) ? dynamicConfig[index] : config[index];
    }
}

using Microsoft.Extensions.Configuration;

namespace Generellem.Services;

public class DynamicConfiguration : IDynamicConfiguration
{
    readonly IConfiguration config;

    readonly Dictionary<string, string?> dynamicConfig = new();

    public DynamicConfiguration(IConfiguration config)
    {
        this.config = config;
    }

    public string? this[string index]
    {
        get => dynamicConfig.ContainsKey(index) ? dynamicConfig[index] : config[index];
        set => dynamicConfig[index] = value;
    }
}

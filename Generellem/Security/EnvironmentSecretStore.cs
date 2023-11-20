namespace Generellem.Security;

/// <summary>
/// For getting secrets from environmnt variables
/// </summary>
public class EnvironmentSecretStore : ISecretStore
{
    public string this[string key] 
    {
        get => Environment.GetEnvironmentVariable(key) ?? string.Empty;
        set => Environment.SetEnvironmentVariable(value, key); 
    }
}

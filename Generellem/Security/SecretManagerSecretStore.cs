using Microsoft.Extensions.Configuration;

namespace Generellem.Security;

/// <summary>
/// Supports the .NET Secret Manager for developer secrets
/// </summary>
public class SecretManagerSecretStore : ISecretStore
{
    const string ArgumentErrorMessage = """
        IConfigurationRoot hasn't been set properly, you can add it to your configuration like this:

        #if DEBUG
            configBuilder.AddUserSecrets<Program>();
        #endif
        
        and then pass it into this constructor when building your IoC container, like this:

        services.AddTransient<ISecretManager>(svc => new SecretManagerSecrets(config));

        """;
    const string KeyNotFoundErrorMessage = """
        Couldn't find the secret, based on the key you used.

        Here's what you can do to debug:

        1. Check to see if the key is listed and double-check for spelling mistakes and/or spaces

        >dotnet user-secrets list

        2. Set the key if it isn't there:

        >dotnet set "<key name>" "<key value>"

        3. If the key is there, but misspelled, do this and then set the key again, as described in #2:

        >dotnet remove "<key name>"

        """;

    readonly IConfigurationRoot config;

    public SecretManagerSecretStore(IConfigurationRoot config)
    {
        this.config = config;   
    }

    public string this[string key] 
    {
        get
        {
            if (config is null)
                throw new ArgumentNullException(nameof(config), ArgumentErrorMessage);

            string? secret = config[key];

            if (secret is null)
                throw new KeyNotFoundException(@"Key: '{key}': " + KeyNotFoundErrorMessage);

            return secret;
        }
        set
        {
            if (config is null)
                throw new ArgumentNullException(nameof(config), ArgumentErrorMessage);

            config[key] = value;
        }
    }
}

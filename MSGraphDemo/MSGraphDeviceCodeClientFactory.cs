using Azure.Identity;

using Generellem.DocumentSource;
using Generellem.Services;

using Microsoft.Graph;

namespace MSGraphDemo;

/// <summary>
/// TODO: This is your code that provides a GraphServiceClient for MSGraph Access.
/// TODO: Visit https://learn.microsoft.com/en-us/graph/overview for design choices that make sense for you.
/// </summary>
public class MSGraphDeviceCodeClientFactory(IDynamicConfiguration config) : IMSGraphClientFactory
{
    /// <summary>
    /// Instantiates a new <see cref="GraphServiceClient"/> for accessing MSGraph.
    /// </summary>
    /// <returns><see cref="GraphServiceClient"/>.</returns>
    public async Task<GraphServiceClient> CreateAsync(string scopes)
    {
        // Multi-tenant apps can use "common",
        // single-tenant apps must use the tenant ID from the Azure portal
        var tenantId = "common";

        // TODO: Add MSGraphClientID as an environment variable
        // Value from app registration
        var clientId = config[GKeys.MSGraphClientID];

        // using Azure.Identity;
        var options = new DeviceCodeCredentialOptions
        {
            //AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
            ClientId = clientId,
            TenantId = tenantId,
            // Callback function that receives the user prompt
            // Prompt contains the generated device code that user must
            // enter during the auth process in the browser
            DeviceCodeCallback = (code, cancellation) =>
            {
                Console.WriteLine(code.Message);
                return Task.FromResult(0);
            },
        };

        // https://learn.microsoft.com/dotnet/api/azure.identity.devicecodecredential
        DeviceCodeCredential deviceCodeCredential = new(options);

        GraphServiceClient graphClient = new(deviceCodeCredential, scopes.Split(' '));

        return await Task.FromResult(graphClient);
    }
}

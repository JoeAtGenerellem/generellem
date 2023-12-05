namespace Generellem.Services;

public class GKeys
{
    //
    // Azure Blob Service
    //

    public static string AzBlobConnectionString { get; set; } = "GenerellemAzBlobConnectionString";
    public static string AzBlobContainer { get; set; } = "GenerellemAzBlobContainer";

    //
    // Azure OpenAI
    //

    public static string AzOpenAIApiKey { get; set; } = "GenerellemAzOpenAIApiKey";
    public static string AzOpenAIDeploymentName { get; set; } = "GenerellemAzOpenAIDeploymentName";
    public static string AzOpenAIEmbeddingName { get; set; } = "GenerellemAzOpenAIEmbeddingName";
    public static string AzOpenAIEndpointName { get; set; } = "GenerellemAzOpenAIEndpointName";

    //
    // Azure Cognitive Search
    //

    public static string AzSearchServiceAdminApiKey { get; set; } = "GenerellemAzSearchServiceAdminApiKey";
    public static string AzSearchServiceEndpoint { get; set; } = "GenerellemAzSearchServiceEndpoint";
    public static string AzSearchServiceIndex { get; set; } = "GenerellemAzSearchServiceIndex";
}

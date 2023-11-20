using Generellem.DataSource;
using Generellem.Llm;
using Generellem.Llm.AzureOpenAI;
using Generellem.Orchestrator;
using Generellem.Rag;
using Generellem.Security;
using Generellem.Services.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

IConfigurationRoot config = InitializeConfiguration();

ServiceCollection services = ConfigureServices();

ServiceProvider svcProvider = services.BuildServiceProvider();

GenerellemOrchestrator orchestrator = svcProvider.GetRequiredService<GenerellemOrchestrator>();

CancellationTokenSource tokenSource = new();

await orchestrator.ProcessFilesAsync(tokenSource.Token);

string response = await orchestrator.AskAsync("What is Generative AI?", tokenSource.Token);

Console.WriteLine("\nResponse:\n");
Console.WriteLine(response);

static IConfigurationRoot InitializeConfiguration()
{
    IConfigurationBuilder configBuilder = new ConfigurationBuilder()
        .AddJsonFile($"appsettings.json", true, true)
        .AddEnvironmentVariables();

#if DEBUG
    configBuilder.AddUserSecrets<Program>();
#endif

    return configBuilder.Build();
}

ServiceCollection ConfigureServices()
{
    ServiceCollection services = new();

    services.AddTransient<IAzureBlobService>(svc => new AzureBlobService(Environment.GetEnvironmentVariable("GenerellemBlobConnectionString")!, Environment.GetEnvironmentVariable("GenerellemBlobContainer")!));
    services.AddTransient<IAzureSearchService, AzureSearchService>();
    services.AddTransient<IDocumentSource, FileSystem>();
    services.AddTransient<ILlm, AzureOpenAILlm>();
    services.AddTransient<IRag, AzureOpenAIRag>();
    services.AddTransient<GenerellemOrchestrator, AzureOpenAIOrchestrator>();
    //services.AddTransient<ISecretStore>(svc => new SecretManagerSecretStore(config));
    services.AddTransient<ISecretStore>(svc => new EnvironmentSecretStore());

    return services;
}

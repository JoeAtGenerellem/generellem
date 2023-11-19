using Generellem.DataSource;
using Generellem.Llm;
using Generellem.Llm.AzureOpenAI;
using Generellem.Orchestrator;
using Generellem.Rag;
using Generellem.Services;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

InitializeConfiguration();

ServiceCollection services = ConfigureServices();

ServiceProvider svcProvider = services.BuildServiceProvider();

GenerellemOrchestrator orchestrator = svcProvider.GetRequiredService<GenerellemOrchestrator>();

CancellationTokenSource tokenSource = new();

await orchestrator.ProcessFilesAsync(tokenSource.Token);

string response = await orchestrator.AskAsync("What is Generative AI?", tokenSource.Token);

Console.WriteLine("\nResponse:\n");
Console.WriteLine(response);

static void InitializeConfiguration()
{
    IConfigurationBuilder configBuilder = new ConfigurationBuilder()
        .AddJsonFile($"appsettings.json", true, true)
        .AddEnvironmentVariables();

    var configurationRoot = configBuilder.Build();
}

static ServiceCollection ConfigureServices()
{
    ServiceCollection services = new();

    services.AddTransient<IAzureBlobService>(svc => new AzureBlobService(Environment.GetEnvironmentVariable("GenerellemBlobConnectionString")!, Environment.GetEnvironmentVariable("GenerellemBlobContainer")!));
    services.AddTransient<IAzureSearchService, AzureSearchService>();
    services.AddTransient<IDocumentSource, FileSystem>();
    services.AddTransient<ILlm, AzureOpenAILlm>();
    services.AddTransient<IRag, AzureOpenAIRag>();
    services.AddTransient<GenerellemOrchestrator, AzureOpenAIOrchestrator>();
    return services;
}

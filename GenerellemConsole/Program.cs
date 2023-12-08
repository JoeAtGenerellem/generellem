using Azure;
using Azure.AI.OpenAI;

using Generellem.DataSource;
using Generellem.Llm;
using Generellem.Llm.AzureOpenAI;
using Generellem.Orchestrator;
using Generellem.Rag;
using Generellem.Rag.AzureOpenAI;
using Generellem.Services.Azure;

using GenerellemConsole;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

CancellationTokenSource tokenSource = new();

IHost host = InitializeConfiguration(args);
await host.RunAsync(tokenSource.Token);

IHost InitializeConfiguration(string[] args)
{
    HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

    builder.Configuration.Sources.Clear();

    IHostEnvironment env = builder.Environment;

    builder.Configuration
        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
        .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true, true)
        .AddEnvironmentVariables();

    ConfigureServices(builder.Services);

#if DEBUG
    builder.Configuration.AddUserSecrets<Program>();
#endif

    return builder.Build();
}

void ConfigureServices(IServiceCollection services)
{
    services.AddHostedService<GenerellemHostedService>();

    services.AddTransient<LlmClientFactory, LlmClientFactory>();
    services.AddTransient<IAzureSearchService, AzureSearchService>();
    services.AddTransient<IDocumentSource, FileSystem>();
    services.AddTransient<ILlm, AzureOpenAILlm>();
    services.AddTransient<IRag, AzureOpenAIRag>();
    services.AddTransient<GenerellemOrchestratorBase, AzureOpenAIOrchestrator>();
}

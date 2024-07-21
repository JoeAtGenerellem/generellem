using Generellem.DocumentSource;
using Generellem.Embedding;
using Generellem.Embedding.AzureOpenAI;
using Generellem.Llm;
using Generellem.Llm.AzureOpenAI;
using Generellem.Processors;
using Generellem.Rag;
using Generellem.Rag.AzureOpenAI;
using Generellem.Repository;
using Generellem.Services;
using Generellem.Services.Azure;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using MSGraphDemo;

//
// TODO: You need to ensure you have a /Documents/Genrellem folder, with files, for the Microsoft Account that you're logging into.
// TODO: Failure to put files in this location will result in an error and/or the sample not working because it can't ingest files.
//
//[
//  {
//    "description": "OneDrive Generellem Files",
//    "path": "/drive/root:/Documents/Generellem"
//  }
//]

CancellationTokenSource tokenSource = new();

IHost host = InitializeConfiguration(args);

// config file location for this demo only
GenerellemFiles.SubFolder = "MSGraphDemo";

ConfigureDB();

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

void ConfigureDB()
{
    using GenerellemContext ctx = new(host.Services.GetRequiredService<IGenerellemFiles>());

    try
    {
        ctx.Database.OpenConnection();
        ctx.Database.Migrate();
    }
    catch (Exception)
    {
        Console.WriteLine("Migration is up-to-date.");
    }
}

void ConfigureServices(IServiceCollection services)
{
    services.AddHostedService<GenerellemHostedService>();

    services.AddTransient<GenerellemContext>();
    services.AddTransient<LlmClientFactory, LlmClientFactory>();

    services.AddTransient<IAzureSearchService, AzureSearchService>();
    services.AddTransient<IDocumentHashRepository, DocumentHashRepository>();
    services.AddTransient<IDocumentSourceFactory, MSGraphDocumentSourceFactory>();
    services.AddTransient<IDynamicConfiguration, DynamicConfiguration>();
    services.AddTransient<IEmbedding, AzureOpenAIEmbedding>();
    services.AddTransient<IGenerellemFiles, GenerellemFiles>();
    services.AddTransient<IGenerellemIngestion, Ingestion>();
    services.AddTransient<IGenerellemQuery, AzureOpenAIQuery>();
    services.AddTransient<IHttpClientFactory, HttpClientFactory>();
    services.AddTransient<ILlm, AzureOpenAILlm>();
    services.AddTransient<IMSGraphClientFactory, MSGraphDeviceCodeClientFactory>();
    services.AddTransient<IPathProviderFactory, PathProviderFactory>();
    services.AddTransient<IRag, AzureOpenAIRag>();
}

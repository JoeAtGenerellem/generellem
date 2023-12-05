using Generellem.Orchestrator;

using Microsoft.Extensions.Hosting;

namespace GenerellemConsole;

internal class GenerellemHostedService : IHostedService
{
    readonly GenerellemOrchestratorBase orchestrator;
    readonly IHostApplicationLifetime lifetime;

    public GenerellemHostedService(GenerellemOrchestratorBase orchestrator, IHostApplicationLifetime lifetime)
    {
        this.orchestrator = orchestrator;
        this.lifetime = lifetime;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        CancellationTokenSource tokenSource = new();

        await orchestrator.ProcessFilesAsync(tokenSource.Token);

        string response = await orchestrator.AskAsync("How do I contribute to Generellem?", tokenSource.Token);

        Console.WriteLine("\nResponse:\n");
        Console.WriteLine(response);

        lifetime.StopApplication();

        do
        {
            // If the user runs outside of visual studio
            // the application runs and stops before they
            // can see output. Although this causes a second
            // keypress of Visual studio, it still helps when
            // used on the Windows command line, another
            // platform like Linux or MacOS, or when using an
            // editor that doesn't stop the temp command window.
            Console.WriteLine("\nPress Enter to stop...\n");
            ConsoleKeyInfo keyInfo = Console.ReadKey();

            if (keyInfo.Key == ConsoleKey.Enter)
                break;

        } while (true);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
    }
}

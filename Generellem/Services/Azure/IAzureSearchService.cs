namespace Generellem.Services.Azure;

public interface IAzureSearchService
{
    Task RunIndexerAsync();
}
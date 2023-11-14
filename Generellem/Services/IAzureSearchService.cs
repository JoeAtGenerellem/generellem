namespace Generellem.Services;

public interface IAzureSearchService
{
    Task RunIndexerAsync();
}
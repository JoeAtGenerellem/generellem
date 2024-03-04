namespace Generellem.Services;

public interface IGenerellemConfiguration
{
    string? this[string index] { get; }
}
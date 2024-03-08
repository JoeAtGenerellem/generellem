namespace Generellem.Services;

public interface IDynamicConfiguration
{
    string? this[string index] { get; set; }
}
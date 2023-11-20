namespace Generellem.Security;

public interface ISecretStore
{
    string this[string key] { get; set; }
}

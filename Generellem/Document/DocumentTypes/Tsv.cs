namespace Generellem.Document.DocumentTypes;

public class Tsv : Text
{
    public override List<string> SupportedExtensions => new() { ".tsv" };
}

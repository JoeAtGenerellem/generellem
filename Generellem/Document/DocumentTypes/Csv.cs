namespace Generellem.Document.DocumentTypes;

public class Csv : Text
{
    public override List<string> SupportedExtensions => new() { ".csv" };
}

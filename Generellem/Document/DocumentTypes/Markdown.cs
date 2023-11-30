namespace Generellem.Document.DocumentTypes;

public class Markdown : Text
{
    public override List<string> SupportedExtensions => new() { ".markdown", ".mdown", ".mkdn", ".mkd", ".mdwn", ".md" };
}

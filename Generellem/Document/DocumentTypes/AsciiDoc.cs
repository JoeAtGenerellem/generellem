namespace Generellem.Document.DocumentTypes;

public class AsciiDoc : Text
{
    public override List<string> SupportedExtensions => new() { ".adoc", ".asc", ".asciidoc" };
}

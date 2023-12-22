using Generellem.Document;
using Generellem.Document.DocumentTypes;

namespace Generellem.DocTypes.Tests;

public class DocumentTypeFactoryTests
{
    [Theory]
    [InlineData("document.adoc", typeof(AsciiDoc))]
    [InlineData("document.asc", typeof(AsciiDoc))]
    [InlineData("document.asciidoc", typeof(AsciiDoc))]
    [InlineData("document.html", typeof(Html))]
    [InlineData("document.md", typeof(Markdown))]
    [InlineData("document.pdf", typeof(Pdf))]
    //[InlineData("document.pptx", typeof(Powerpoint))]
    //[InlineData("document.ppt", typeof(Powerpoint))]
    [InlineData("document.txt", typeof(Text))]
    [InlineData("document.docx", typeof(Word))]
    [InlineData("document.doc", typeof(Word))]
    [InlineData("document.xyz", typeof(Unknown))]
    [InlineData("document.", typeof(Unknown))]
    [InlineData("document", typeof(Unknown))]
    public void Create_ValidExtensions_ReturnsCorrectDocumentType(string fileName, Type expectedType)
    {
        var documentType = DocumentTypeFactory.Create(fileName);

        Assert.IsType(expectedType, documentType);
    }
}
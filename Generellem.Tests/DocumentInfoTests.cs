using Generellem.Document.DocumentTypes;

namespace Generellem.DocumentSource.Tests
{
    public class DocumentInfoTests
    {
        [Fact]
        public void Constructor_WithValidArgs_SetsProperties()
        {
            string ExpectedDocSource = $"{Environment.MachineName}:{nameof(FileSystem)}";
            string ExpectedFilePath = "/path/to/file.txt";
            MemoryStream docStream = new();
            Text docType = new();

            DocumentInfo docInfo = new(ExpectedDocSource, docStream, docType, ExpectedFilePath);

            Assert.Equal($"{ExpectedDocSource}@{ExpectedFilePath}", docInfo.FileRef);
            Assert.Same(docStream, docInfo.DocStream);
            Assert.Same(docType, docInfo.DocType);
        }

        [Fact]
        public void Constructor_WithNullArgs_SetsDefaults()
        {
            var docInfo = new DocumentInfo(null, null, null, null);

            Assert.Equal("@", docInfo.FileRef);
            Assert.Null(docInfo.DocStream);
            Assert.Null(docInfo.DocType);
        }
    }
}

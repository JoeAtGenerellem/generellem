using Generellem.Services;

namespace Generellem.DocumentSource.Tests;

public class FileSystemTests
{
    readonly Mock<IPathProvider> pathProviderMock = new();
    readonly Mock<IPathProviderFactory> pathProviderFactoryMock = new();

    public FileSystemTests()
    {
        pathProviderFactoryMock
            .Setup(fact => fact.Create(It.IsAny<IDocumentSource>()))
            .Returns(pathProviderMock.Object);
    }

    [Fact]
    public async Task GetFiles_ReturnsFiles()
    {
        PathSpec fileSpec = new() { Path = "." };
        pathProviderMock
            .Setup(fs => fs.GetPathsAsync(It.IsAny<string>()))
            .ReturnsAsync(new[] { fileSpec });

        var fileSystem = new FileSystem(pathProviderFactoryMock.Object);

        await foreach (DocumentInfo docInfo in fileSystem.GetDocumentsAsync(CancellationToken.None))
            Assert.NotEmpty(docInfo.DocumentReference);
    }
}

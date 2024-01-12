namespace Generellem.DocumentSource.Tests;

public class FileSystemTests
{
    [Fact]
    public async Task GetFiles_ReturnsFiles()
    {
        FileSpec fileSpec = new() { Path = "C:\\Project" };
        var mockGetPaths = new Mock<FileSystem>();
        mockGetPaths.Setup(fs => fs.GetPathsAsync(It.IsAny<string>()))
                    .ReturnsAsync(new[] { fileSpec });

        var fileSystem = new FileSystem();

        await foreach (DocumentInfo docInfo in fileSystem.GetDocumentsAsync(CancellationToken.None))
            Assert.NotEmpty(docInfo.DocumentReference);
    }

    [Theory]
    [InlineData("\\bin")]
    [InlineData("\\obj")]
    public void IsPathExcluded_DetectsExcludedPaths(string path)
    {
        var fileSystem = new FileSystem();

        var result = fileSystem.IsPathExcluded(path);

        Assert.True(result);
    }
}

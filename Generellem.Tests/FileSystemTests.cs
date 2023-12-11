namespace Generellem.DocumentSource.Tests;

public class FileSystemTests
{
    [Fact]
    public void GetFiles_ReturnsFiles()
    {
        FileSpec fileSpec = new() { Path = "C:\\Project" };
        var mockGetPaths = new Mock<FileSystem>();
        mockGetPaths.Setup(fs => fs.GetPaths(It.IsAny<string>()))
                    .Returns(new[] { fileSpec });

        var fileSystem = new FileSystem();

        var result = fileSystem.GetFiles(CancellationToken.None);

        Assert.NotEmpty(result);
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

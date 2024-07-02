using Generellem.Services;

namespace Generellem.DocumentSource.Tests;

public class FilePathProviderTests
{
    [Theory]
    [InlineData("\\bin")]
    [InlineData("\\obj")]
    public void IsPathExcluded_DetectsExcludedPaths(string path)
    {
        Mock<IGenerellemFiles> gemFiles = new();

        FilePathProvider provider = new(gemFiles.Object);

        var result = provider.IsPathExcluded(path);

        Assert.True(result);
    }
}

using Xunit;
using Moq;
using Generellem.Document.DocumentTypes;

public class ExcelTests
{
    readonly Excel excel = new();

    [Fact]
    public void CanProcess_ReturnsTrue()
    {
        Assert.True(excel.CanProcess);
    }

    [Fact]
    public async Task GetTextAsync_WithXlsFile_ReturnsExcelText()
    {
        var fileName = "TestDocs/ExcelDoc2.xls";
        using FileStream fileStr = File.Open(fileName, FileMode.Open);

        var result = await excel.GetTextAsync(fileStr, fileName);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task GetTextAsync_WithXlsxFile_ReturnsExcelText()
    {
        var fileName = "TestDocs/ExcelDoc3.xlsx";
        using FileStream fileStr = File.Open(fileName, FileMode.Open);

        var result = await excel.GetTextAsync(fileStr, fileName);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task GetTextAsync_WithNonExcelFile_ThrowsException()
    {
        var fileName = "TestDocs/file1.txt";
        using FileStream fileStr = File.Open(fileName, FileMode.Open);

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await excel.GetTextAsync(fileStr, fileName));
    }
}

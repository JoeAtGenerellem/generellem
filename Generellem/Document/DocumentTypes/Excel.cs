using System.Text;

using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace Generellem.Document.DocumentTypes;

public class Excel : IDocumentType
{
    public virtual bool CanProcess => true;

    public virtual List<string> SupportedExtensions => new() { ".xlsx", ".xls" };

    public virtual async Task<string> GetTextAsync(Stream documentStream, string fileName)
    {
        IWorkbook workbook = Path.GetExtension(fileName) switch
        {
            ".xls" => new HSSFWorkbook(documentStream),
            ".xlsx" => new XSSFWorkbook(documentStream),
            _ => throw new ArgumentException("The specified file is not an Excel file"),
        };

        StringBuilder sb = new();

        for (int i = 0; i < workbook.NumberOfSheets; i++)
        {
            var sheet = workbook.GetSheetAt(i);
            foreach (IRow row in sheet)
            {
                foreach (ICell cell in row)
                {
                    sb.Append(cell.ToString());
                    sb.Append("\t");
                }
                sb.AppendLine();
            }
        }

        return await Task.FromResult(sb.ToString());
    }
}

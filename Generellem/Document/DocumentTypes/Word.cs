using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Generellem.Document.DocumentTypes;
public class Word : IDocumentType
{
    public bool CanProcess { get; set; } = true;

    public List<string> SupportedExtensions => new() { ".docx", ".doc" };

    public string GetText(string path) => throw new NotImplementedException();
}

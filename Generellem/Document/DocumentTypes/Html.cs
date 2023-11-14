using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Generellem.Document.DocumentTypes;

public class Html : IDocumentType
{
    public bool CanProcess { get; set; } = true;

    public List<string> SupportedExtensions => new() { ".html", ".htm" };

    public string GetText(string path) => throw new NotImplementedException();
}

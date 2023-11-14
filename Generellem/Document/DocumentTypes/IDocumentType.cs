using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Generellem.Document.DocumentTypes;

public interface IDocumentType
{
    public bool CanProcess { get; set; }
    string GetText(string path);
    List<string> SupportedExtensions { get; }
}

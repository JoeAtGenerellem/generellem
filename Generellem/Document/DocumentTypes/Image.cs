using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Generellem.Document.DocumentTypes;
public class Image : IDocumentType
{
    public bool CanProcess { get; set; } = false;

    public List<string> SupportedExtensions => new() { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };

    public string GetText(string path) => throw new NotImplementedException();
}

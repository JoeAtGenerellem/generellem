using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Generellem.Document.DocumentTypes;
public class Audio : IDocumentType
{
    public bool CanProcess { get; set; } = false;

    public List<string> SupportedExtensions => new() { ".mp3", ".wav", ".ogg" };

    public string GetText(string path) => throw new NotImplementedException();
}

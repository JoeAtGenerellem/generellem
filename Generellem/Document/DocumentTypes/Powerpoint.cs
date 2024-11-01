using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;

using System.Text;

namespace Generellem.Document.DocumentTypes;

public class Powerpoint : IDocumentType
{
    public virtual bool CanProcess => true;

    public virtual List<string> SupportedExtensions => new() { ".pptx"/*, ".ppt"*/ };

    /// <summary>
    /// Pulls text from a PowerPoint presentation
    /// </summary>
    /// <param name="documentStream"><see cref="Stream"/> of PowerPoint document</param>
    /// <param name="fileName">Name of PowerPoint file</param>
    /// <returns>String representation of PowerPoint file.</returns>
    public virtual async Task<string> GetTextAsync(Stream documentStream, string fileName)
    {
        ArgumentNullException.ThrowIfNull(documentStream, nameof(documentStream));
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName, nameof(fileName));

        using PresentationDocument presentationDocument = PresentationDocument.Open(documentStream, false);

        PresentationPart? presentationPart = presentationDocument.PresentationPart;

        StringBuilder sb = new();

        if (presentationPart is not null)
            foreach (SlidePart slidePart in presentationPart.SlideParts)
                ProcessSlide(slidePart, sb);

        return await Task.FromResult(sb.ToString());
    }

    void ProcessSlide(SlidePart slidePart, StringBuilder sb)
    {
        if (slidePart?.Slide?.CommonSlideData is CommonSlideData slideData)
            if (slideData?.ShapeTree is ShapeTree shapeTree)
                foreach (var shape in shapeTree.Elements<Shape>())
                    foreach (var text in shape.Descendants<DocumentFormat.OpenXml.Drawing.Text>())
                        sb.AppendLine(text.InnerText);
    }
}

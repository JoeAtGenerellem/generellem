using System.Text;

namespace Generellem.Rag;

/// <summary>
/// Various methods for processing text
/// </summary>
public class TextProcessor
{
    /// <summary>
    /// Breaks a string into chunks with overlap.
    /// </summary>
    /// <param name="text">Full text string to break apart.</param>
    /// <param name="documentReference">Reference to file. e.g. either a path, url, or some other indicator of where the file came from</param>
    /// <returns>List of <see cref="TextChunk"/> representing input <see cref="text"/></returns>
    public static List<TextChunk> BreakIntoChunks(string text, string documentReference)
    {
        List<TextChunk> chunks = [];
        
        int chunkSize = 5000;
        int overlap = 100;
        
        for(int i = 0; i < text.Length; i += (chunkSize - overlap)) 
        {
            int start = i;
            int end = Math.Min(i + chunkSize, text.Length);
            
            string content = text[start..end];

            chunks.Add(
                new TextChunk()
                {
                    ID = Convert.ToBase64String(Encoding.UTF8.GetBytes(documentReference)),
                    Content = content,
                    DocumentReference = documentReference
                });
        }
        
        return chunks;
    }
}

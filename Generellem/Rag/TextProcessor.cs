using System.Text;
using Generellem.Embedding;

namespace Generellem.Rag;

/// <summary>
/// Various methods for processing text
/// </summary>
public class TextProcessor
{
    /// <summary>
    /// Maximum size per text chunk. (default = 5000)
    /// </summary>
    public static int ChunkSize { get; set; } = 5000;

    /// <summary>
    /// Number of characters to overlap between chunks. (default = 100)
    /// </summary>
    public static int Overlap { get; set; } = 100;

    /// <summary>
    /// Breaks a string into chunks with overlap.
    /// </summary>
    /// <param name="text">Full text string to break apart.</param>
    /// <param name="documentReference">Reference to file. e.g. either a path, url, or some other indicator of where the file came from</param>
    /// <returns>List of <see cref="TextChunk"/> representing input <see cref="text"/></returns>
    public static List<TextChunk> BreakIntoChunks(string text, string documentReference)
    {
        List<TextChunk> chunks = [];
        
        int chunkSize = Math.Min(text.Length, Math.Max(0, ChunkSize));
        int overlap = chunkSize < Overlap ? 0 : Overlap;

        for(int i = 0; i < text.Length; i += (chunkSize - overlap)) 
        {
            int start = i;
            int end = Math.Min(i + ChunkSize, text.Length);
            
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

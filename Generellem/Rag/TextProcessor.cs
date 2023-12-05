using System.Security.Cryptography;
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
    /// <param name="fileRef">Reference to file. e.g. either a path, url, or some other indicator of where the file came from</param>
    /// <returns>List of <see cref="TextChunk"/> representing input <see cref="text"/></returns>
    public static List<TextChunk> BreakIntoChunks(string text, string fileRef)
    {
        List<TextChunk> chunks = new();
        
        int chunkSize = 5000;
        int overlap = 100;
        
        for(int i = 0; i < text.Length; i += (chunkSize - overlap)) 
        {
            int start = i;
            int end = Math.Min(i + chunkSize, text.Length);
            
            string content = text.Substring(start, end - start);

            chunks.Add(
                new TextChunk()
                {
                    ID = Guid.NewGuid().ToString(),// GenerateUniqueTextChunkID(content, fileRef),
                    Content = content,
                    FileRef = fileRef
                });
        }
        
        return chunks;
    }

    /// <summary>
    /// Hash the content to get a unique ID
    /// </summary>
    /// <param name="content">Text chunk content</param>
    /// <param name="fileRef">Reference to file. e.g. either a path, url, or some other indicator of where the file came from</param>
    /// <returns></returns>
    static string GenerateUniqueTextChunkID(string content, string fileRef)
    {
        // sometimes files are copies of the same content
        // and this avoids duplicate IDs
        string uniqueContent = content + fileRef;

        // no security requirement here
        // just get hash with fast algorithm
        using var md5 = MD5.Create();
        byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(uniqueContent));

        // this gets rid of strange characters
        // that could cause problems with Azure Search
        return Convert.ToBase64String(hash, 0, hash.Length);
    }
}

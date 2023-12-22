using System.Text;

using Azure.AI.OpenAI;

using Generellem.DocumentSource;
using Generellem.Llm;
using Generellem.Rag;

namespace Generellem.Orchestrator;

/// <summary>
/// Coordinates <see cref="IDocumentSource"/>, <see cref="ILlm"/>, and <see cref="IRag"/>
/// to coordinate document processing and querying.
/// </summary>
public abstract class GenerellemOrchestratorBase
{
    protected virtual IEnumerable<IDocumentSource> DocSources { get; init; }
    protected virtual ILlm Llm { get; init; }
    protected virtual IRag Rag { get; init; }

    public GenerellemOrchestratorBase(IDocumentSourceFactory docSourceFact, ILlm llm, IRag rag)
    {
        this.DocSources = docSourceFact.GetDocumentSources();
        this.Llm = llm;
        this.Rag = rag;

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public abstract Task ProcessFilesAsync(CancellationToken cancellationToken);

    public abstract Task<string> AskAsync(string message, Queue<ChatMessage> chatHistory, CancellationToken  cancellationToken);
}

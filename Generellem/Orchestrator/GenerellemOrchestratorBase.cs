using Azure.AI.OpenAI;

using Generellem.DocumentSource;
using Generellem.Llm;
using Generellem.Rag;

namespace Generellem.Orchestrator;

/// <summary>
/// Coordinates <see cref="IDocumentSource"/>, <see cref="ILlm"/>, and <see cref="IRag"/>
/// to coordinate document processing and querying.
/// </summary>
public abstract class GenerellemOrchestratorBase(IDocumentSource docSource, ILlm llm, IRag rag)
{
    protected virtual IDocumentSource DocSource { get; init; } = docSource;
    protected virtual ILlm Llm { get; init; } = llm;
    protected virtual IRag Rag { get; init; } = rag;

    public abstract Task ProcessFilesAsync(CancellationToken cancellationToken);

    public abstract Task<string> AskAsync(string message, Queue<ChatMessage> chatHistory, CancellationToken  cancellationToken);
}

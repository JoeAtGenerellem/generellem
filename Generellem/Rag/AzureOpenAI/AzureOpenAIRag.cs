using Generellem.Services.Exceptions;

using Polly;

namespace Generellem.Rag.AzureOpenAI;

/// <summary>
/// Performs Retrieval-Augmented Generation (RAG) for Azure OpenAI.
/// </summary>
public class AzureOpenAIRag() 
    : IRag
{
    readonly ResiliencePipeline pipeline = 
        new ResiliencePipelineBuilder()
            .AddRetry(new()
             {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(ex => ex is not GenerellemNeedsIngestionException)
             })
            .AddTimeout(TimeSpan.FromSeconds(7))
            .Build();
}

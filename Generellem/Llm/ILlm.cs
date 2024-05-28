using Polly;

namespace Generellem.Llm;

public interface ILlm
{
    ResiliencePipeline Pipeline { get; set; }

    Task<TResponse> PromptAsync<TResponse>(IChatRequest request, CancellationToken cancellationToken)
        where TResponse: IChatResponse;
}

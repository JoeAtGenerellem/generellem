namespace Generellem.Llm;

public interface ILlm
{
    Task<TResponse> AskAsync<TResponse>(IChatRequest request, CancellationToken cancellationToken)
        where TResponse: IChatResponse;
}

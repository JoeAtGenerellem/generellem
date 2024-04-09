using Generellem.Llm;

namespace Generellem.Processors;

public class QueryDetails<TRequest, TResponse>
    where TRequest : IChatRequest
    where TResponse : IChatResponse
{
    public TRequest? Request { get; set; }

    public TResponse? Response { get; set; }
}

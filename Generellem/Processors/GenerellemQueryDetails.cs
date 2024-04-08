using Generellem.Llm;

namespace Generellem.Processors;

public class GenerellemQueryDetails<TRequest, TResponse>
    where TRequest : IChatRequest
    where TResponse : IChatResponse
{
    public TRequest? Request { get; set; }

    public TResponse? Response { get; set; }
}

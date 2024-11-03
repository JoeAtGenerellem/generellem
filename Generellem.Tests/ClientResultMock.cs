using System.ClientModel;
using System.ClientModel.Primitives;

namespace Generellem.Tests;

public class ClientResultMock<T> : ClientResult<T>
{
    public ClientResultMock() : this(default, new PipeLineResponseMock())
    {
    }
    public ClientResultMock(T? value, PipelineResponse? response) : base(value!, response!)
    {
    }
}

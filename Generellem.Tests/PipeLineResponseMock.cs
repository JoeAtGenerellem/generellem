using System.ClientModel.Primitives;

namespace Generellem.Tests;

public class PipeLineResponseMock : PipelineResponse
{
    public PipeLineResponseMock() : base()
    {
    }

    public override int Status => throw new NotImplementedException();

    public override string ReasonPhrase => throw new NotImplementedException();

    public override Stream? ContentStream { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public override BinaryData Content => throw new NotImplementedException();

    protected override PipelineResponseHeaders HeadersCore => throw new NotImplementedException();

    public override BinaryData BufferContent(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override ValueTask<BinaryData> BufferContentAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override void Dispose()
    {
        throw new NotImplementedException();
    }
}

namespace Generellem.Services.Exceptions;

public class GenerellemNeedsIngestionException : Exception
{
    public GenerellemNeedsIngestionException(string message) : base(message)
    {
    }

    public GenerellemNeedsIngestionException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

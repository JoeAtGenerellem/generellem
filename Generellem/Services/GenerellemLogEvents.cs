using Microsoft.Extensions.Logging;

namespace Generellem.Services;

/// <summary>
/// Events that appear in Generellem ILogger output
/// </summary>
public static class GenerellemLogEvents
{
    /// <summary>
    /// Unable to log in
    /// </summary>
    public static readonly EventId AuthenticationFailure = new(4401, "Authentication Error");

    /// <summary>
    /// User doesn't have permissions for a resource
    /// </summary>
    public static readonly EventId AuthorizationFailure = new(4403, "Authorization Failure");

    /// <summary>
    /// Caller cancelled the request
    /// </summary>
    public static readonly EventId Cancelled = new(4499, "Cancelled");

    /// <summary>
    /// Received an error during an HTTP request
    /// </summary>
    public static readonly EventId HttpError = new(4400, "HTTP Request Error");

    /// <summary>
    /// Progress/Status updates
    /// </summary>
    public static readonly EventId Information = new(1411, "Information");

    /// <summary>
    /// We don't have enough information to classify - look at exception details
    /// </summary>
    public static readonly EventId SystemFailure = new(5500, "Unknown System Failure");
}
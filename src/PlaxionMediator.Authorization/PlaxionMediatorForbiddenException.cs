using PlaxionMediator.Core;

namespace PlaxionMediator.Authorization;

/// <summary>
/// Thrown by <see cref="AuthorizationBehavior{TRequest,TResponse}"/> when a check returns
/// <see cref="AuthorizationOutcome.Forbidden"/>.
/// </summary>
public sealed class PlaxionMediatorForbiddenException : PlaxionMediatorException
{
    /// <summary>
    /// Initializes a new instance with the default message.
    /// </summary>
    /// <param name="requestTypeName">The name of the request type that failed authorization.</param>
    public PlaxionMediatorForbiddenException(string requestTypeName)
        : base($"Forbidden: caller is not permitted to execute request '{requestTypeName}'.")
    {
        RequestTypeName = requestTypeName;
    }

    /// <summary>
    /// Initializes a new instance with a custom message.
    /// </summary>
    /// <param name="requestTypeName">The name of the request type that failed authorization.</param>
    /// <param name="message">A custom error message.</param>
    public PlaxionMediatorForbiddenException(string requestTypeName, string message)
        : base(message)
    {
        RequestTypeName = requestTypeName;
    }

    /// <summary>
    /// The name of the request type that failed authorization.
    /// </summary>
    public string RequestTypeName { get; }
}

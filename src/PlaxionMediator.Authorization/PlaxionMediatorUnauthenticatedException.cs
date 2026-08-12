using PlaxionMediator.Core;

namespace PlaxionMediator.Authorization;

/// <summary>
/// Thrown by <see cref="AuthorizationBehavior{TRequest,TResponse}"/> when a check returns
/// <see cref="AuthorizationOutcome.Unauthenticated"/>.
/// </summary>
public sealed class PlaxionMediatorUnauthenticatedException : PlaxionMediatorException
{
    /// <summary>
    /// Initializes a new instance with the default message.
    /// </summary>
    /// <param name="requestTypeName">The name of the request type that failed authorization.</param>
    public PlaxionMediatorUnauthenticatedException(string requestTypeName)
        : base($"Unauthenticated: caller could not be authenticated for request '{requestTypeName}'.")
    {
        RequestTypeName = requestTypeName;
    }

    /// <summary>
    /// Initializes a new instance with a custom message.
    /// </summary>
    /// <param name="requestTypeName">The name of the request type that failed authorization.</param>
    /// <param name="message">A custom error message.</param>
    public PlaxionMediatorUnauthenticatedException(string requestTypeName, string message)
        : base(message)
    {
        RequestTypeName = requestTypeName;
    }

    /// <summary>
    /// The name of the request type that failed authorization.
    /// </summary>
    public string RequestTypeName { get; }
}

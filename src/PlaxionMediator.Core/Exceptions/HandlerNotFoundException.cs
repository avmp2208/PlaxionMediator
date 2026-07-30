namespace PlaxionMediator.Core;

/// <summary>
/// Defensive exception for the theoretically-unreachable missing-handler case.
/// Handler existence is normally guaranteed at compile time by PlaxionMediator001.
/// </summary>
public sealed class HandlerNotFoundException : PlaxionMediatorException
{
    /// <summary>
    /// Initializes a new instance for the given request type.
    /// </summary>
    public HandlerNotFoundException(Type requestType)
        : base($"No handler registered for request type '{requestType.FullName ?? requestType.Name}'.")
    {
        RequestType = requestType;
    }

    /// <summary>
    /// Initializes a new instance with a custom message.
    /// </summary>
    public HandlerNotFoundException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// The request type that had no handler, when known.
    /// </summary>
    public Type? RequestType { get; }
}

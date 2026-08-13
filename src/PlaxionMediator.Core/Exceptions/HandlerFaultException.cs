namespace PlaxionMediator.Core;

/// <summary>
/// Internal marker wrapping an exception raised by the terminal handler invocation.
/// </summary>
internal sealed class HandlerFaultException : PlaxionMediatorException
{
    /// <summary>
    /// Initializes a new instance wrapping the given fault.
    /// </summary>
    public HandlerFaultException(Exception inner)
        : base(inner.Message, inner)
    {
    }

    /// <summary>
    /// Initializes a new instance wrapping the given fault with request diagnostic context.
    /// </summary>
    public HandlerFaultException(Exception inner, string? requestTypeName)
        : base(inner.Message, inner)
    {
        RequestTypeName = requestTypeName;
    }

    /// <summary>
    /// Optional name of the request type being processed when the failure occurred.
    /// Additive diagnostic context; never populated with request payload data to avoid leaking
    /// sensitive information by default.
    /// </summary>
    public string? RequestTypeName { get; }
}

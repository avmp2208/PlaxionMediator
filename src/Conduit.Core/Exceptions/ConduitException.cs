namespace Conduit.Core;

/// <summary>
/// Common base so consumers can catch any Conduit-originated failure in one clause.
/// </summary>
public abstract class ConduitException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConduitException"/> class.
    /// </summary>
    protected ConduitException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConduitException"/> class with a message.
    /// </summary>
    protected ConduitException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConduitException"/> class with a message and inner exception.
    /// </summary>
    protected ConduitException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

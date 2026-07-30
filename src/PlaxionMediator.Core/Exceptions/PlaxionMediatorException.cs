namespace PlaxionMediator.Core;

/// <summary>
/// Common base so consumers can catch any PlaxionMediator-originated failure in one clause.
/// </summary>
public abstract class PlaxionMediatorException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlaxionMediatorException"/> class.
    /// </summary>
    protected PlaxionMediatorException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaxionMediatorException"/> class with a message.
    /// </summary>
    protected PlaxionMediatorException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaxionMediatorException"/> class with a message and inner exception.
    /// </summary>
    protected PlaxionMediatorException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

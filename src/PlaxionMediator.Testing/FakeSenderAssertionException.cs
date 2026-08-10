namespace PlaxionMediator.Testing;

/// <summary>
/// Thrown by <see cref="FakeSender"/> assertion helpers (e.g. <see cref="FakeSender.AssertCallCount{TRequest}"/>,
/// <see cref="FakeSender.AssertSentInOrder"/>) when the recorded requests do not match expectations.
/// </summary>
public sealed class FakeSenderAssertionException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FakeSenderAssertionException"/> class.
    /// </summary>
    public FakeSenderAssertionException(string message)
        : base(message)
    {
    }
}

namespace PlaxionMediator.Authorization;

/// <summary>
/// The outcome of evaluating an <see cref="IRequestAuthorization{TRequest}"/> check.
/// </summary>
public enum AuthorizationOutcome
{
    /// <summary>The caller is authorized to execute the request.</summary>
    Authorized,

    /// <summary>The caller could not be authenticated (no/invalid identity).</summary>
    Unauthenticated,

    /// <summary>The caller is authenticated but not permitted to execute the request.</summary>
    Forbidden,
}

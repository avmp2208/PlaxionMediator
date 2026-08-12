namespace PlaxionMediator.Authorization;

/// <summary>
/// A single authorization check for requests of type <typeparamref name="TRequest"/>.
/// Multiple implementations may be registered for the same request type; <see cref="AuthorizationBehavior{TRequest,TResponse}"/>
/// evaluates them with deterministic AND semantics, short-circuiting on the first non-<see cref="AuthorizationOutcome.Authorized"/> result.
/// </summary>
/// <typeparam name="TRequest">The request type this check applies to.</typeparam>
public interface IRequestAuthorization<in TRequest>
{
    /// <summary>
    /// Evaluates whether the current caller is authorized to execute <paramref name="request"/>.
    /// </summary>
    /// <param name="request">The request being authorized.</param>
    /// <param name="context">The current authorization context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The outcome of the authorization check.</returns>
    ValueTask<AuthorizationOutcome> AuthorizeAsync(
        TRequest request,
        IAuthorizationContext context,
        CancellationToken cancellationToken);
}

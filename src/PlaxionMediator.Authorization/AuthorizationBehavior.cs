using System.Diagnostics;
using PlaxionMediator.Abstractions;

namespace PlaxionMediator.Authorization;

/// <summary>
/// Pipeline behavior that runs registered <see cref="IRequestAuthorization{TRequest}"/> checks
/// before the handler is executed.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public sealed class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IRequestAuthorization<TRequest>> _checks;
    private readonly IAuthorizationContextAccessor _contextAccessor;
    private static readonly KeyValuePair<string, object?>[] _tags =
    {
        new(AuthorizationMeter.RequestTypeTag, typeof(TRequest).FullName)
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthorizationBehavior{TRequest,TResponse}"/> class.
    /// </summary>
    /// <param name="checks">The authorization checks registered for <typeparamref name="TRequest"/>.</param>
    /// <param name="contextAccessor">The accessor for the current authorization context.</param>
    public AuthorizationBehavior(
        IEnumerable<IRequestAuthorization<TRequest>> checks,
        IAuthorizationContextAccessor contextAccessor)
    {
        _checks = checks ?? throw new ArgumentNullException(nameof(checks));
        _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
    }

    /// <inheritdoc />
    public async ValueTask<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(next);
        cancellationToken.ThrowIfCancellationRequested();

        // Fast path: no authorization checks registered for this request type.
        // Avoid resolving context when there are no checks.
        if (_checks is ICollection<IRequestAuthorization<TRequest>> collection)
        {
            if (collection.Count == 0)
            {
                return await next().ConfigureAwait(false);
            }
        }
        else if (!_checks.Any())
        {
            return await next().ConfigureAwait(false);
        }

        long startTimestamp = Stopwatch.GetTimestamp();
        try
        {
            IAuthorizationContext context = _contextAccessor.Current;

            foreach (IRequestAuthorization<TRequest> check in _checks)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (check is null)
                {
                    continue;
                }

                AuthorizationOutcome outcome = await check
                    .AuthorizeAsync(request, context, cancellationToken)
                    .ConfigureAwait(false);

                if (outcome == AuthorizationOutcome.Unauthenticated)
                {
                    AuthorizationMeter.DeniedCount.Add(1, _tags);
                    throw new PlaxionMediatorUnauthenticatedException(typeof(TRequest).Name);
                }

                if (outcome == AuthorizationOutcome.Forbidden)
                {
                    AuthorizationMeter.DeniedCount.Add(1, _tags);
                    throw new PlaxionMediatorForbiddenException(typeof(TRequest).Name);
                }
            }

            AuthorizationMeter.AllowedCount.Add(1, _tags);
            return await next().ConfigureAwait(false);
        }
        finally
        {
            TimeSpan elapsed = Stopwatch.GetElapsedTime(startTimestamp);
            AuthorizationMeter.Duration.Record(elapsed.TotalMilliseconds, _tags);
        }
    }
}

using PlaxionMediator.Abstractions;

namespace PlaxionMediator.Transactions;

/// <summary>
/// Pipeline behavior that wraps the inner pipeline in a transaction for requests implementing
/// <see cref="ITransactionalRequest"/>. Non-transactional requests are a fast no-op.
/// </summary>
/// <remarks>
/// Recommended pipeline order (outer → inner): Validation → Authorization → Retry → Transaction → Handler
/// so each retry attempt receives a fresh transaction. Failed attempts roll back before the next retry.
/// Nested <c>Send</c> / existing transactions use "join don't nest" semantics via
/// <see cref="ITransactionScope.HasExistingTransaction"/>.
/// </remarks>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
internal sealed class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ITransactionManager _transactionManager;
    private readonly PlaxionMediatorTransactionOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionBehavior{TRequest,TResponse}"/> class.
    /// </summary>
    public TransactionBehavior(
        ITransactionManager transactionManager,
        PlaxionMediatorTransactionOptions options)
    {
        _transactionManager = transactionManager ?? throw new ArgumentNullException(nameof(transactionManager));
        _options = options ?? throw new ArgumentNullException(nameof(options));
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

        // Fast path: open-generic registration applies to every request; only opt-in types open a transaction.
        if (request is not ITransactionalRequest transactional)
        {
            return await next().ConfigureAwait(false);
        }

        TransactionIsolationLevel isolationLevel = transactional.IsolationLevel ?? _options.DefaultIsolationLevel;

        ITransactionScope scope = await _transactionManager
            .BeginAsync(isolationLevel, cancellationToken)
            .ConfigureAwait(false);

        try
        {
            TResponse response = await next().ConfigureAwait(false);

            await scope.CommitAsync(cancellationToken).ConfigureAwait(false);
            return response;
        }
        catch
        {
            try
            {
                await scope.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch
            {
                // Preserve the original exception from the handler/pipeline; rollback failures are secondary.
            }

            throw;
        }
        finally
        {
            await scope.DisposeAsync().ConfigureAwait(false);
        }
    }
}

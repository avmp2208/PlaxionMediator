using System.Collections.Concurrent;
using Conduit.Abstractions;
using Conduit.Core;

namespace Conduit.Testing;

/// <summary>
/// In-memory <see cref="ISender"/> that records sent requests and returns pre-programmed responses.
/// </summary>
public sealed class FakeSender : ISender
{
    private readonly ConcurrentDictionary<Type, Func<object, CancellationToken, ValueTask<object?>>> _handlers = new();
    private readonly List<object> _sentRequests = [];
    private readonly object _gate = new();

    /// <summary>
    /// Requests observed by <see cref="Send{TResponse}"/>, in call order.
    /// </summary>
    public IReadOnlyList<object> SentRequests
    {
        get
        {
            lock (_gate)
            {
                return _sentRequests.ToArray();
            }
        }
    }

    /// <summary>
    /// Registers a synchronous response factory for <typeparamref name="TRequest"/>.
    /// </summary>
    public void When<TRequest, TResponse>(Func<TRequest, TResponse> respond)
        where TRequest : IRequest<TResponse>
    {
        ArgumentNullException.ThrowIfNull(respond);
        When<TRequest, TResponse>((request, _) => ValueTask.FromResult(respond(request)));
    }

    /// <summary>
    /// Registers an asynchronous response factory for <typeparamref name="TRequest"/>.
    /// </summary>
    public void When<TRequest, TResponse>(Func<TRequest, CancellationToken, ValueTask<TResponse>> respond)
        where TRequest : IRequest<TResponse>
    {
        ArgumentNullException.ThrowIfNull(respond);

        _handlers[typeof(TRequest)] = async (request, ct) =>
        {
            TResponse response = await respond((TRequest)request, ct).ConfigureAwait(false);
            return response;
        };
    }

    /// <inheritdoc />
    public ValueTask<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        lock (_gate)
        {
            _sentRequests.Add(request);
        }

        Type requestType = request.GetType();
        if (!_handlers.TryGetValue(requestType, out Func<object, CancellationToken, ValueTask<object?>>? handler))
        {
            throw new HandlerNotFoundException(requestType);
        }

        return SendCoreAsync(handler, request, cancellationToken);

        static async ValueTask<TResponse> SendCoreAsync(
            Func<object, CancellationToken, ValueTask<object?>> handler,
            IRequest<TResponse> request,
            CancellationToken cancellationToken)
        {
            object? result = await handler(request, cancellationToken).ConfigureAwait(false);
            return (TResponse)result!;
        }
    }

    /// <summary>
    /// Clears recorded requests and registered handlers.
    /// </summary>
    public void Reset()
    {
        _handlers.Clear();
        lock (_gate)
        {
            _sentRequests.Clear();
        }
    }
}

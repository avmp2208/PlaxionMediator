namespace PlaxionMediator.Abstractions;

/// <summary>
/// Marks a type as a dispatchable request that produces a <typeparamref name="TResponse"/>.
/// Prefer implementing this on a <c>sealed record</c> for immutability.
/// </summary>
/// <typeparam name="TResponse">The response type produced when the request is handled.</typeparam>
public interface IRequest<out TResponse>;

/// <summary>
/// Convenience marker for requests that produce no meaningful response (<see cref="Unit"/>).
/// </summary>
public interface IRequest : IRequest<Unit>;

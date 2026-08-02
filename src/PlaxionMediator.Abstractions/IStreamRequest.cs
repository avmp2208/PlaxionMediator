namespace PlaxionMediator.Abstractions;

/// <summary>
/// Marks a type as a streaming request that produces a sequence of <typeparamref name="TResponse"/> items.
/// Prefer implementing this on a <c>sealed record</c> for immutability.
/// </summary>
/// <typeparam name="TResponse">The element type yielded by the stream.</typeparam>
public interface IStreamRequest<out TResponse>;

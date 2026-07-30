namespace PlaxionMediator.Abstractions;

/// <summary>
/// The "next" continuation a pipeline behavior invokes to proceed down the pipeline.
/// </summary>
/// <typeparam name="TResponse">The response type flowing through the pipeline.</typeparam>
public delegate ValueTask<TResponse> RequestHandlerDelegate<TResponse>();

using Microsoft.CodeAnalysis;

namespace PlaxionMediator.Analyzers;

/// <summary>
/// Analyzer diagnostics.
/// PlaxionMediator001–006 (existing), PlaxionMediator011/020–022/031–032/040–041/080–083/090 (Phase C).
/// </summary>
public static class DiagnosticDescriptors
{
    public const string MissingHandlerId = "PlaxionMediator001";
    public const string MultipleHandlersId = "PlaxionMediator002";
    public const string MutableRequestId = "PlaxionMediator003";
    public const string MissingCancellationTokenId = "PlaxionMediator004";
    public const string MissingRequestBindingAttributeId = "PlaxionMediator005";
    public const string HandlerBlockingCallId = "PlaxionMediator006";

    public const string NonSealedHandlerId = "PlaxionMediator011";
    public const string InvalidBehaviorRegistrationId = "PlaxionMediator020";
    public const string DuplicateRegistrationId = "PlaxionMediator021";
    public const string IncorrectLifetimeId = "PlaxionMediator022";
    public const string MissingCancellationTokenPropagationId = "PlaxionMediator031";
    public const string CancellationTokenNoneUsageId = "PlaxionMediator032";
    public const string AsyncVoidHandlerId = "PlaxionMediator040";
    public const string HandlerDependsOnISenderSelfTypeId = "PlaxionMediator041";
    public const string UnnecessaryBehaviorOnHotPathId = "PlaxionMediator080";
    public const string SynchronousOnlyHandlerId = "PlaxionMediator081";
    public const string BehaviorAllocatesInHotPathId = "PlaxionMediator082";
    public const string StreamHandlerBuffersSequenceId = "PlaxionMediator083";
    public const string NotificationHandlerThrowsWithoutAwaitingOthersId = "PlaxionMediator090";

    public static readonly DiagnosticDescriptor MissingHandler = new(
        id: MissingHandlerId,
        title: "Missing handler",
        messageFormat: "Request type '{0}' does not have a corresponding IRequestHandler implementation",
        category: "PlaxionMediator.Registration",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Every IRequest<TResponse> must have exactly one IRequestHandler implementation.",
        customTags: [WellKnownDiagnosticTags.CompilationEnd]);

    public static readonly DiagnosticDescriptor MultipleHandlers = new(
        id: MultipleHandlersId,
        title: "Multiple handlers",
        messageFormat: "Request type '{0}' has multiple IRequestHandler implementations",
        category: "PlaxionMediator.Registration",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Exactly one IRequestHandler must exist per request type.",
        customTags: [WellKnownDiagnosticTags.CompilationEnd]);

    public static readonly DiagnosticDescriptor MutableRequest = new(
        id: MutableRequestId,
        title: "Mutable request",
        messageFormat: "Request type '{0}' should be an immutable sealed record (found mutable public setters)",
        category: "PlaxionMediator.Immutability",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Requests must be immutable. Prefer sealed record types without settable properties.");

    public static readonly DiagnosticDescriptor MissingCancellationToken = new(
        id: MissingCancellationTokenId,
        title: "Missing CancellationToken parameter",
        messageFormat: "Handler method '{0}' is missing a CancellationToken parameter",
        category: "PlaxionMediator.Cancellation",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "IRequestHandler.Handle and INotificationHandler.Handle should accept a CancellationToken.");

    public static readonly DiagnosticDescriptor MissingRequestBindingAttribute = new(
        id: MissingRequestBindingAttributeId,
        title: "Request type has no bindable members",
        messageFormat: "Request type '{0}' used with {1} has no public constructor parameters or public properties to bind from route/query values",
        category: "PlaxionMediator.AspNetCore",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "MapPlaxionMediatorGet/Delete bind TRequest from route and query values. The request type should expose public primary-constructor parameters or public properties.");

    public static readonly DiagnosticDescriptor HandlerBlockingCall = new(
        id: HandlerBlockingCallId,
        title: "Blocking call inside handler",
        messageFormat: "Avoid blocking call '{0}' inside handler method '{1}'; prefer awaiting asynchronously",
        category: "PlaxionMediator.Concurrency",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Sync-over-async patterns (.Result, .Wait(), .GetAwaiter().GetResult()) inside handlers can cause deadlocks and thread-pool starvation.");

    public static readonly DiagnosticDescriptor NonSealedHandler = new(
        id: NonSealedHandlerId,
        title: "Non-sealed handler",
        messageFormat: "Handler type '{0}' should be sealed to prevent accidental subclassing that bypasses DI-registered behavior",
        category: "PlaxionMediator.Registration",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A handler class is not sealed, allowing accidental subclassing that bypasses DI-registered behavior.");

    public static readonly DiagnosticDescriptor InvalidBehaviorRegistration = new(
        id: InvalidBehaviorRegistrationId,
        title: "Invalid behavior registration",
        messageFormat: "Type '{0}' does not implement IPipelineBehavior<,> and cannot be registered with PipelineBuilder.Use",
        category: "PlaxionMediator.Pipeline",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "PipelineBuilder.Use<T>() must be called with a type that implements IPipelineBehavior<,>.");

    public static readonly DiagnosticDescriptor DuplicateRegistration = new(
        id: DuplicateRegistrationId,
        title: "Duplicate registration",
        messageFormat: "Behavior type '{0}' is registered more than once for the same pipeline",
        category: "PlaxionMediator.Pipeline",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The same behavior type registered twice for the same request or pipeline is redundant and may change ordering unexpectedly.");

    public static readonly DiagnosticDescriptor IncorrectLifetime = new(
        id: IncorrectLifetimeId,
        title: "Incorrect lifetime",
        messageFormat: "Singleton handler/behavior '{0}' captures dependency '{1}' which is registered as {2}",
        category: "PlaxionMediator.Lifetime",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A Singleton-lifetime behavior/handler captures a Scoped or Transient dependency via constructor injection.",
        customTags: [WellKnownDiagnosticTags.CompilationEnd]);

    public static readonly DiagnosticDescriptor MissingCancellationTokenPropagation = new(
        id: MissingCancellationTokenPropagationId,
        title: "Missing CancellationToken propagation",
        messageFormat: "CancellationToken parameter '{0}' is not passed to awaited call '{1}' that accepts a CancellationToken",
        category: "PlaxionMediator.Cancellation",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A handler/behavior receives a CancellationToken parameter but doesn't pass it to an awaited async call that accepts one.");

    public static readonly DiagnosticDescriptor CancellationTokenNoneUsage = new(
        id: CancellationTokenNoneUsageId,
        title: "CancellationToken.None usage",
        messageFormat: "CancellationToken.None is used inside handler method '{0}' where an ambient CancellationToken is available",
        category: "PlaxionMediator.Cancellation",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "CancellationToken.None used inside a handler where the ambient token is available.");

    public static readonly DiagnosticDescriptor AsyncVoidHandler = new(
        id: AsyncVoidHandlerId,
        title: "Async void handler",
        messageFormat: "Handler or behavior method '{0}' is declared async void; use ValueTask/Task instead",
        category: "PlaxionMediator.Concurrency",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A handler or behavior method is declared async void, which prevents proper exception observation.");

    public static readonly DiagnosticDescriptor HandlerDependsOnISenderSelfType = new(
        id: HandlerDependsOnISenderSelfTypeId,
        title: "Handler depends on ISender for self-type",
        messageFormat: "Handler '{0}' sends request type '{1}' which it also handles, risking infinite recursion",
        category: "PlaxionMediator.Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A handler injects ISender and sends a request of its own type (risk of infinite recursion).");

    public static readonly DiagnosticDescriptor UnnecessaryBehaviorOnHotPath = new(
        id: UnnecessaryBehaviorOnHotPathId,
        title: "Unnecessary behavior on hot-path request",
        messageFormat: "High-frequency request '{0}' has {1} pipeline behaviors registered; consider narrowing with UseWhen",
        category: "PlaxionMediator.Performance",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "A request marked [HighFrequency] has more than N behaviors in its chain (default threshold: 3).",
        customTags: [WellKnownDiagnosticTags.CompilationEnd]);

    public static readonly DiagnosticDescriptor SynchronousOnlyHandler = new(
        id: SynchronousOnlyHandlerId,
        title: "Synchronous-only handler",
        messageFormat: "Handler method '{0}' has no await; consider returning ValueTask.FromResult for synchronous completion",
        category: "PlaxionMediator.Performance",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Handler body has no await — suggests using a synchronous-completion-optimized pattern.");

    public static readonly DiagnosticDescriptor BehaviorAllocatesInHotPath = new(
        id: BehaviorAllocatesInHotPathId,
        title: "Behavior allocates in hot path",
        messageFormat: "Behavior method '{0}' allocates '{1}' per call; consider caching or pooling",
        category: "PlaxionMediator.Performance",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "A behavior allocates a new closure/collection per call detectable via a simple data-flow heuristic.");

    public static readonly DiagnosticDescriptor StreamHandlerBuffersSequence = new(
        id: StreamHandlerBuffersSequenceId,
        title: "Stream handler buffers entire sequence",
        messageFormat: "Stream handler method '{0}' materializes the sequence via '{1}' before yielding, defeating streaming",
        category: "PlaxionMediator.Performance",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A stream handler that buffers the entire sequence (List/array/ToList/ToArray) before yielding defeats the point of IAsyncEnumerable streaming.");

    public static readonly DiagnosticDescriptor NotificationHandlerThrowsWithoutAwaitingOthers = new(
        id: NotificationHandlerThrowsWithoutAwaitingOthersId,
        title: "Notification handler throws without awaiting others",
        messageFormat: "Notification handler '{0}' uses a fail-fast throw pattern that is incompatible with notification fan-out semantics",
        category: "PlaxionMediator.Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "A notification handler's exception handling pattern suggests fail-fast assumptions incompatible with fan-out semantics.");
}

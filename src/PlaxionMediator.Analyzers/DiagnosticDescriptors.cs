using Microsoft.CodeAnalysis;

namespace PlaxionMediator.Analyzers;

/// <summary>
/// Analyzer diagnostics. IDs follow the catalog:
/// PlaxionMediator001 Missing Handler, PlaxionMediator002 Multiple Handlers,
/// PlaxionMediator003 Mutable Request, PlaxionMediator004 Missing CancellationToken,
/// PlaxionMediator005 Missing Request Binding Surface, PlaxionMediator006 Handler Blocking Call.
/// Note: PlaxionMediator001/002 are also reported by the source generator at build time;
/// analyzers provide IDE-time feedback for the same rules.
/// </summary>
public static class DiagnosticDescriptors
{
    public const string MissingHandlerId = "PlaxionMediator001";
    public const string MultipleHandlersId = "PlaxionMediator002";
    public const string MutableRequestId = "PlaxionMediator003";
    public const string MissingCancellationTokenId = "PlaxionMediator004";
    public const string MissingRequestBindingAttributeId = "PlaxionMediator005";
    public const string HandlerBlockingCallId = "PlaxionMediator006";

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
}

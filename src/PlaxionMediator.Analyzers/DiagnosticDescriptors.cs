using Microsoft.CodeAnalysis;

namespace PlaxionMediator.Analyzers;

/// <summary>
/// MVP analyzer diagnostics. IDs follow the issue MVP catalog:
/// PlaxionMediator001 Missing Handler, PlaxionMediator002 Multiple Handlers,
/// PlaxionMediator003 Mutable Request, PlaxionMediator004 Missing CancellationToken.
/// Note: PlaxionMediator001/002 are also reported by the source generator at build time;
/// analyzers provide IDE-time feedback for the same rules.
/// </summary>
public static class DiagnosticDescriptors
{
    public const string MissingHandlerId = "PlaxionMediator001";
    public const string MultipleHandlersId = "PlaxionMediator002";
    public const string MutableRequestId = "PlaxionMediator003";
    public const string MissingCancellationTokenId = "PlaxionMediator004";

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
}

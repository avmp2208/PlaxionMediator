using Microsoft.CodeAnalysis;

namespace PlaxionMediator.SourceGenerators;

internal static class Diagnostics
{
    public const string MissingHandlerId = "PlaxionMediator001";
    public const string MultipleHandlersId = "PlaxionMediator002";

    public static readonly DiagnosticDescriptor MissingHandler = new(
        id: MissingHandlerId,
        title: "Missing handler",
        messageFormat: "Request type '{0}' does not have a corresponding IRequestHandler implementation",
        category: "PlaxionMediator.Registration",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Every IRequest<TResponse> must have exactly one IRequestHandler<TRequest, TResponse> in the compilation.");

    public static readonly DiagnosticDescriptor MultipleHandlers = new(
        id: MultipleHandlersId,
        title: "Multiple handlers",
        messageFormat: "Request type '{0}' has multiple IRequestHandler implementations",
        category: "PlaxionMediator.Registration",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Exactly one IRequestHandler<TRequest, TResponse> must exist per request type.");
}

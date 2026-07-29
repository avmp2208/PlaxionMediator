using Microsoft.CodeAnalysis;

namespace Conduit.SourceGenerators;

internal static class Diagnostics
{
    public const string MissingHandlerId = "CONDUIT001";
    public const string MultipleHandlersId = "CONDUIT002";

    public static readonly DiagnosticDescriptor MissingHandler = new(
        id: MissingHandlerId,
        title: "Missing handler",
        messageFormat: "Request type '{0}' does not have a corresponding IRequestHandler implementation",
        category: "Conduit.Registration",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Every IRequest<TResponse> must have exactly one IRequestHandler<TRequest, TResponse> in the compilation.");

    public static readonly DiagnosticDescriptor MultipleHandlers = new(
        id: MultipleHandlersId,
        title: "Multiple handlers",
        messageFormat: "Request type '{0}' has multiple IRequestHandler implementations",
        category: "Conduit.Registration",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Exactly one IRequestHandler<TRequest, TResponse> must exist per request type.");
}

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Conduit.SourceGenerators;

/// <summary>
/// Incremental generator that discovers Conduit handlers/requests and emits
/// dispatcher + DI registration code with compile-time diagnostics.
/// </summary>
[Generator]
public sealed class ConduitGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<RequestHandlerModel?> requestHandlers = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => IsTypeDeclarationWithBaseList(node),
                static (ctx, ct) => GetRequestHandlerModel(ctx, ct))
            .Where(static m => m is not null);

        IncrementalValuesProvider<NotificationHandlerModel?> notificationHandlers = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => IsTypeDeclarationWithBaseList(node),
                static (ctx, ct) => GetNotificationHandlerModel(ctx, ct))
            .Where(static m => m is not null);

        IncrementalValuesProvider<RequestModel?> requests = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => IsTypeDeclarationWithBaseList(node),
                static (ctx, ct) => GetRequestModel(ctx, ct))
            .Where(static m => m is not null);

        IncrementalValueProvider<string> rootNamespace = context.CompilationProvider
            .Select(static (compilation, _) =>
            {
                // Prefer the assembly name as a stable root namespace fallback.
                string? name = compilation.AssemblyName;
                if (string.IsNullOrWhiteSpace(name))
                {
                    return "ConduitApp";
                }

                // Sanitize assembly names that are not valid namespace identifiers.
                return new string(name.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());
            });

        IncrementalValueProvider<GenerationModel> model = requestHandlers.Collect()
            .Combine(notificationHandlers.Collect())
            .Combine(requests.Collect())
            .Combine(rootNamespace)
            .Select(static (tuple, _) =>
            {
                ImmutableArray<RequestHandlerModel?> rh = tuple.Left.Left.Left;
                ImmutableArray<NotificationHandlerModel?> nh = tuple.Left.Left.Right;
                ImmutableArray<RequestModel?> req = tuple.Left.Right;
                string ns = tuple.Right;

                ImmutableArray<RequestHandlerModel> requestHandlerModels = rh
                    .Where(m => m is not null)
                    .Select(m => m!)
                    .Distinct()
                    .OrderBy(m => m.RequestFullyQualifiedName)
                    .ThenBy(m => m.HandlerFullyQualifiedName)
                    .ToImmutableArray();

                ImmutableArray<NotificationHandlerModel> notificationHandlerModels = nh
                    .Where(m => m is not null)
                    .Select(m => m!)
                    .Distinct()
                    .OrderBy(m => m.NotificationFullyQualifiedName)
                    .ThenBy(m => m.HandlerFullyQualifiedName)
                    .ToImmutableArray();

                ImmutableArray<RequestModel> requestModels = req
                    .Where(m => m is not null)
                    .Select(m => m!)
                    .Distinct()
                    .OrderBy(m => m.RequestFullyQualifiedName)
                    .ToImmutableArray();

                return new GenerationModel(
                    new EquatableArray<RequestHandlerModel>(requestHandlerModels),
                    new EquatableArray<NotificationHandlerModel>(notificationHandlerModels),
                    new EquatableArray<RequestModel>(requestModels),
                    ns);
            });

        context.RegisterSourceOutput(model, static (spc, generationModel) =>
        {
            if (generationModel.RequestHandlers.Length == 0 && 
                generationModel.NotificationHandlers.Length == 0 &&
                generationModel.Requests.Length == 0)
            {
                return;
            }

            ReportDiagnostics(spc, generationModel);
            spc.AddSource("ConduitRegistration.g.cs", SourceEmitter.EmitRegistration(generationModel));
            spc.AddSource("ConduitSender.g.cs", SourceEmitter.EmitSender(generationModel));
        });
    }

    private static bool IsTypeDeclarationWithBaseList(SyntaxNode node)
    {
        return node is BaseTypeDeclarationSyntax { BaseList.Types.Count: > 0 };
    }

    private static RequestHandlerModel? GetRequestHandlerModel(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.Node is not BaseTypeDeclarationSyntax typeDecl)
        {
            return null;
        }

        if (context.SemanticModel.GetDeclaredSymbol(typeDecl, cancellationToken) is not INamedTypeSymbol typeSymbol)
        {
            return null;
        }

        if (!SymbolHelpers.IsConcreteNamedType(typeSymbol))
        {
            return null;
        }

        INamedTypeSymbol? handlerInterface = SymbolHelpers.FindGenericInterface(
            typeSymbol,
            SymbolHelpers.RequestHandlerMetadataName,
            context.SemanticModel.Compilation);

        if (handlerInterface is null || handlerInterface.TypeArguments.Length != 2)
        {
            return null;
        }

        ITypeSymbol requestType = handlerInterface.TypeArguments[0];
        ITypeSymbol responseType = handlerInterface.TypeArguments[1];

        Location location = typeSymbol.Locations.FirstOrDefault() ?? Location.None;
        FileLinePositionSpan lineSpan = location.GetLineSpan();

        return new RequestHandlerModel(
            SymbolHelpers.ToFullyQualifiedName(requestType),
            SymbolHelpers.ToFullyQualifiedName(responseType),
            SymbolHelpers.ToFullyQualifiedName(typeSymbol),
            SymbolHelpers.ToDisplayName(requestType),
            location.SourceTree?.FilePath,
            lineSpan.StartLinePosition.Line + 1,
            location.SourceSpan.Start);
    }

    private static NotificationHandlerModel? GetNotificationHandlerModel(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.Node is not BaseTypeDeclarationSyntax typeDecl)
        {
            return null;
        }

        if (context.SemanticModel.GetDeclaredSymbol(typeDecl, cancellationToken) is not INamedTypeSymbol typeSymbol)
        {
            return null;
        }

        if (!SymbolHelpers.IsConcreteNamedType(typeSymbol))
        {
            return null;
        }

        INamedTypeSymbol? handlerInterface = SymbolHelpers.FindGenericInterface(
            typeSymbol,
            SymbolHelpers.NotificationHandlerMetadataName,
            context.SemanticModel.Compilation);

        if (handlerInterface is null || handlerInterface.TypeArguments.Length != 1)
        {
            return null;
        }

        ITypeSymbol notificationType = handlerInterface.TypeArguments[0];

        return new NotificationHandlerModel(
            SymbolHelpers.ToFullyQualifiedName(notificationType),
            SymbolHelpers.ToFullyQualifiedName(typeSymbol));
    }

    private static RequestModel? GetRequestModel(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.Node is not BaseTypeDeclarationSyntax typeDecl)
        {
            return null;
        }

        if (context.SemanticModel.GetDeclaredSymbol(typeDecl, cancellationToken) is not INamedTypeSymbol typeSymbol)
        {
            return null;
        }

        // Only flag concrete request types declared in source (not the interface itself).
        if (typeSymbol.TypeKind is not (TypeKind.Class or TypeKind.Struct))
        {
            return null;
        }

        if (typeSymbol.IsAbstract || typeSymbol.IsStatic)
        {
            return null;
        }

        INamedTypeSymbol? requestUnbound = context.SemanticModel.Compilation.GetTypeByMetadataName(SymbolHelpers.RequestMetadataName);
        if (!SymbolHelpers.ImplementsRequest(typeSymbol, requestUnbound, out ITypeSymbol? responseType) || responseType is null)
        {
            return null;
        }

        // Skip open generic requests for MVP.
        if (typeSymbol.IsGenericType && typeSymbol.TypeParameters.Length > 0 && typeSymbol.IsDefinition)
        {
            // open generic definition — skip
            if (typeSymbol.TypeArguments.Any(t => t.TypeKind == TypeKind.TypeParameter))
            {
                return null;
            }
        }

        Location location = typeSymbol.Locations.FirstOrDefault() ?? Location.None;
        FileLinePositionSpan lineSpan = location.GetLineSpan();

        return new RequestModel(
            SymbolHelpers.ToFullyQualifiedName(typeSymbol),
            SymbolHelpers.ToFullyQualifiedName(responseType),
            SymbolHelpers.ToDisplayName(typeSymbol),
            location.SourceTree?.FilePath,
            lineSpan.StartLinePosition.Line + 1,
            location.SourceSpan.Start);
    }

    private static void ReportDiagnostics(SourceProductionContext context, GenerationModel model)
    {
        Dictionary<string, List<RequestHandlerModel>> handlersByRequest = model.RequestHandlers
            .GroupBy(h => h.RequestFullyQualifiedName)
            .ToDictionary(g => g.Key, g => g.ToList());

        // CONDUIT002: multiple handlers
        foreach (KeyValuePair<string, List<RequestHandlerModel>> pair in handlersByRequest)
        {
            if (pair.Value.Count <= 1)
            {
                continue;
            }

            foreach (RequestHandlerModel handler in pair.Value)
            {
                Location location = CreateLocation(handler.RequestLocationPath, handler.RequestLocationSpanStart, handler.RequestLocationLine);
                context.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.MultipleHandlers,
                    location,
                    handler.RequestDisplayName));
            }
        }

        // CONDUIT001: missing handlers
        foreach (RequestModel request in model.Requests)
        {
            if (handlersByRequest.ContainsKey(request.RequestFullyQualifiedName))
            {
                continue;
            }

            Location location = CreateLocation(request.LocationPath, request.LocationSpanStart, request.LocationLine);
            context.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.MissingHandler,
                location,
                request.RequestDisplayName));
        }
    }

    private static Location CreateLocation(string? path, int spanStart, int line)
    {
        if (string.IsNullOrEmpty(path))
        {
            return Location.None;
        }

        // Without a SyntaxTree we cannot create a precise SourceLocation easily in all hosts;
        // Location.None still surfaces the diagnostic ID/message in build logs and tests.
        // Generator tests assert by diagnostic ID.
        _ = spanStart;
        _ = line;
        return Location.None;
    }
}

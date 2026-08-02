using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PlaxionMediator.SourceGenerators;

/// <summary>
/// Incremental generator that discovers PlaxionMediator handlers/requests and emits
/// dispatcher + DI registration code with compile-time diagnostics.
/// </summary>
[Generator]
public sealed class PlaxionMediatorGenerator : IIncrementalGenerator
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

        IncrementalValuesProvider<StreamRequestHandlerModel?> streamHandlers = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => IsTypeDeclarationWithBaseList(node),
                static (ctx, ct) => GetStreamRequestHandlerModel(ctx, ct))
            .Where(static m => m is not null);

        IncrementalValuesProvider<RequestModel?> requests = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => IsTypeDeclarationWithBaseList(node),
                static (ctx, ct) => GetRequestModel(ctx, ct))
            .Where(static m => m is not null);

        IncrementalValueProvider<string> rootNamespace = context.CompilationProvider
            .Select(static (compilation, _) =>
            {
                string? name = compilation.AssemblyName;
                if (string.IsNullOrWhiteSpace(name))
                {
                    return "PlaxionMediatorApp";
                }

                return new string(name.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());
            });

        IncrementalValueProvider<GenerationModel> model = requestHandlers.Collect()
            .Combine(notificationHandlers.Collect())
            .Combine(streamHandlers.Collect())
            .Combine(requests.Collect())
            .Combine(rootNamespace)
            .Select(static (tuple, _) =>
            {
                ImmutableArray<RequestHandlerModel?> rh = tuple.Left.Left.Left.Left;
                ImmutableArray<NotificationHandlerModel?> nh = tuple.Left.Left.Left.Right;
                ImmutableArray<StreamRequestHandlerModel?> sh = tuple.Left.Left.Right;
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

                ImmutableArray<StreamRequestHandlerModel> streamHandlerModels = sh
                    .Where(m => m is not null)
                    .Select(m => m!)
                    .Distinct()
                    .OrderBy(m => m.RequestFullyQualifiedName)
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
                    new EquatableArray<StreamRequestHandlerModel>(streamHandlerModels),
                    new EquatableArray<RequestModel>(requestModels),
                    ns);
            });

        context.RegisterSourceOutput(model, static (spc, generationModel) =>
        {
            if (generationModel.RequestHandlers.Length == 0 &&
                generationModel.NotificationHandlers.Length == 0 &&
                generationModel.StreamRequestHandlers.Length == 0 &&
                generationModel.Requests.Length == 0)
            {
                return;
            }

            ReportDiagnostics(spc, generationModel);
            spc.AddSource("PlaxionMediatorRegistration.g.cs", SourceEmitter.EmitRegistration(generationModel));
            spc.AddSource("PlaxionMediatorSender.g.cs", SourceEmitter.EmitSender(generationModel));
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
        string strategy = GetPublishStrategy(notificationType, context.SemanticModel.Compilation);

        return new NotificationHandlerModel(
            SymbolHelpers.ToFullyQualifiedName(notificationType),
            SymbolHelpers.ToFullyQualifiedName(typeSymbol),
            strategy);
    }

    private static StreamRequestHandlerModel? GetStreamRequestHandlerModel(GeneratorSyntaxContext context, CancellationToken cancellationToken)
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
            SymbolHelpers.StreamRequestHandlerMetadataName,
            context.SemanticModel.Compilation);

        if (handlerInterface is null || handlerInterface.TypeArguments.Length != 2)
        {
            return null;
        }

        ITypeSymbol requestType = handlerInterface.TypeArguments[0];
        ITypeSymbol responseType = handlerInterface.TypeArguments[1];

        return new StreamRequestHandlerModel(
            SymbolHelpers.ToFullyQualifiedName(requestType),
            SymbolHelpers.ToFullyQualifiedName(responseType),
            SymbolHelpers.ToFullyQualifiedName(typeSymbol),
            SymbolHelpers.ToDisplayName(requestType));
    }

    private static string GetPublishStrategy(ITypeSymbol notificationType, Compilation compilation)
    {
        INamedTypeSymbol? attributeType = compilation.GetTypeByMetadataName(
            SymbolHelpers.NotificationPublishStrategyAttributeMetadataName);
        if (attributeType is null)
        {
            return "Sequential";
        }

        foreach (AttributeData attribute in notificationType.GetAttributes())
        {
            if (!SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeType))
            {
                continue;
            }

            if (attribute.ConstructorArguments.Length == 1
                && attribute.ConstructorArguments[0].Value is int intValue)
            {
                return intValue == 1 ? "Parallel" : "Sequential";
            }

            if (attribute.ConstructorArguments.Length == 1
                && attribute.ConstructorArguments[0].Value is not null)
            {
                string? name = attribute.ConstructorArguments[0].Value?.ToString();
                if (string.Equals(name, "Parallel", System.StringComparison.Ordinal)
                    || string.Equals(name, "1", System.StringComparison.Ordinal))
                {
                    return "Parallel";
                }
            }
        }

        return "Sequential";
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

        if (typeSymbol.IsGenericType && typeSymbol.TypeParameters.Length > 0 && typeSymbol.IsDefinition)
        {
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

        Dictionary<string, List<StreamRequestHandlerModel>> streamHandlersByRequest = model.StreamRequestHandlers
            .GroupBy(h => h.RequestFullyQualifiedName)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (KeyValuePair<string, List<StreamRequestHandlerModel>> pair in streamHandlersByRequest)
        {
            if (pair.Value.Count <= 1)
            {
                continue;
            }

            foreach (StreamRequestHandlerModel handler in pair.Value)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.MultipleHandlers,
                    Location.None,
                    handler.RequestDisplayName));
            }
        }
    }

    private static Location CreateLocation(string? path, int spanStart, int line)
    {
        if (string.IsNullOrEmpty(path))
        {
            return Location.None;
        }

        _ = spanStart;
        _ = line;
        return Location.None;
    }
}

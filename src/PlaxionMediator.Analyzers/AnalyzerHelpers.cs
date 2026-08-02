using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PlaxionMediator.Analyzers;

internal static class AnalyzerHelpers
{
    public const string RequestMetadataName = "PlaxionMediator.Abstractions.IRequest`1";
    public const string RequestHandlerMetadataName = "PlaxionMediator.Abstractions.IRequestHandler`2";
    public const string NotificationHandlerMetadataName = "PlaxionMediator.Abstractions.INotificationHandler`1";
    public const string StreamRequestHandlerMetadataName = "PlaxionMediator.Abstractions.IStreamRequestHandler`2";
    public const string PipelineBehaviorMetadataName = "PlaxionMediator.Abstractions.IPipelineBehavior`2";
    public const string HighFrequencyAttributeMetadataName = "PlaxionMediator.Abstractions.HighFrequencyAttribute";
    public const string SenderMetadataName = "PlaxionMediator.Core.ISender";
    public const string PipelineBuilderMetadataName = "PlaxionMediator.Pipeline.PipelineBuilder";
    public const int HotPathBehaviorThreshold = 3;

    public static bool ImplementsRequest(INamedTypeSymbol type, INamedTypeSymbol? requestUnbound, out ITypeSymbol? responseType)
    {
        responseType = null;
        if (requestUnbound is null)
        {
            return false;
        }

        foreach (INamedTypeSymbol iface in type.AllInterfaces)
        {
            if (iface.IsGenericType
                && iface.TypeArguments.Length == 1
                && SymbolEqualityComparer.Default.Equals(iface.OriginalDefinition, requestUnbound))
            {
                responseType = iface.TypeArguments[0];
                return true;
            }
        }

        return false;
    }

    public static INamedTypeSymbol? FindRequestHandlerInterface(INamedTypeSymbol type, INamedTypeSymbol? unbound)
    {
        if (unbound is null)
        {
            return null;
        }

        return type.AllInterfaces.FirstOrDefault(i =>
            i.IsGenericType
            && i.TypeArguments.Length == 2
            && SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, unbound));
    }

    public static bool IsConcreteType(INamedTypeSymbol type)
    {
        return type is { IsAbstract: false, IsStatic: false, TypeKind: TypeKind.Class or TypeKind.Struct };
    }

    public static bool ImplementsGenericInterface(INamedTypeSymbol type, INamedTypeSymbol? unbound)
    {
        if (unbound is null)
        {
            return false;
        }

        return type.AllInterfaces.Any(i =>
            i.IsGenericType && SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, unbound));
    }

    public static bool IsHandlerOrBehaviorType(INamedTypeSymbol type, Compilation compilation)
    {
        INamedTypeSymbol? requestHandler = compilation.GetTypeByMetadataName(RequestHandlerMetadataName);
        INamedTypeSymbol? notificationHandler = compilation.GetTypeByMetadataName(NotificationHandlerMetadataName);
        INamedTypeSymbol? streamHandler = compilation.GetTypeByMetadataName(StreamRequestHandlerMetadataName);
        INamedTypeSymbol? behavior = compilation.GetTypeByMetadataName(PipelineBehaviorMetadataName);

        return ImplementsGenericInterface(type, requestHandler)
               || ImplementsGenericInterface(type, notificationHandler)
               || ImplementsGenericInterface(type, streamHandler)
               || ImplementsGenericInterface(type, behavior);
    }

    public static bool IsHandlerType(INamedTypeSymbol type, Compilation compilation)
    {
        INamedTypeSymbol? requestHandler = compilation.GetTypeByMetadataName(RequestHandlerMetadataName);
        INamedTypeSymbol? notificationHandler = compilation.GetTypeByMetadataName(NotificationHandlerMetadataName);
        INamedTypeSymbol? streamHandler = compilation.GetTypeByMetadataName(StreamRequestHandlerMetadataName);

        return ImplementsGenericInterface(type, requestHandler)
               || ImplementsGenericInterface(type, notificationHandler)
               || ImplementsGenericInterface(type, streamHandler);
    }

    public static bool IsNotificationHandlerType(INamedTypeSymbol type, Compilation compilation)
    {
        INamedTypeSymbol? notificationHandler = compilation.GetTypeByMetadataName(NotificationHandlerMetadataName);
        return ImplementsGenericInterface(type, notificationHandler);
    }

    public static bool IsStreamHandlerType(INamedTypeSymbol type, Compilation compilation)
    {
        INamedTypeSymbol? streamHandler = compilation.GetTypeByMetadataName(StreamRequestHandlerMetadataName);
        return ImplementsGenericInterface(type, streamHandler);
    }

    public static bool IsBehaviorType(INamedTypeSymbol type, Compilation compilation)
    {
        INamedTypeSymbol? behavior = compilation.GetTypeByMetadataName(PipelineBehaviorMetadataName);
        return ImplementsGenericInterface(type, behavior);
    }

    public static bool IsHandleMethod(IMethodSymbol method)
    {
        if (method.MethodKind is MethodKind.ExplicitInterfaceImplementation)
        {
            return method.Name is "Handle"
                   || method.ExplicitInterfaceImplementations.Any(impl => impl.Name == "Handle");
        }

        return method is { Name: "Handle", MethodKind: MethodKind.Ordinary };
    }

    public static IMethodSymbol? GetEnclosingMethod(SemanticModel model, SyntaxNode node)
    {
        ISymbol? symbol = model.GetEnclosingSymbol(node.SpanStart);
        while (symbol is not null)
        {
            if (symbol is IMethodSymbol method)
            {
                return method;
            }

            symbol = symbol.ContainingSymbol;
        }

        return null;
    }

    public static bool MethodHasCancellationToken(IMethodSymbol method, INamedTypeSymbol? cancellationTokenType)
    {
        if (cancellationTokenType is null)
        {
            return false;
        }

        return method.Parameters.Any(p => SymbolEqualityComparer.Default.Equals(p.Type, cancellationTokenType));
    }

    public static string? GetSimpleName(ExpressionSyntax expression)
    {
        return expression switch
        {
            IdentifierNameSyntax id => id.Identifier.Text,
            MemberAccessExpressionSyntax member => member.Name.Identifier.Text,
            GenericNameSyntax generic => generic.Identifier.Text,
            _ => null,
        };
    }
}

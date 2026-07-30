using System.Linq;
using Microsoft.CodeAnalysis;

namespace PlaxionMediator.SourceGenerators;

internal static class SymbolHelpers
{
    public const string RequestHandlerMetadataName = "PlaxionMediator.Abstractions.IRequestHandler`2";
    public const string NotificationHandlerMetadataName = "PlaxionMediator.Abstractions.INotificationHandler`1";
    public const string RequestMetadataName = "PlaxionMediator.Abstractions.IRequest`1";
    public const string NotificationMetadataName = "PlaxionMediator.Abstractions.INotification";

    public static string ToFullyQualifiedName(ITypeSymbol symbol)
    {
        return symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    public static string ToDisplayName(ITypeSymbol symbol)
    {
        return symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
    }

    public static bool IsConcreteNamedType(INamedTypeSymbol type)
    {
        return type is { IsAbstract: false, IsStatic: false, TypeKind: TypeKind.Class or TypeKind.Struct }
               && !type.IsGenericType;
    }

    public static INamedTypeSymbol? FindInterface(INamedTypeSymbol type, INamedTypeSymbol unboundInterface)
    {
        foreach (INamedTypeSymbol iface in type.AllInterfaces)
        {
            if (iface.IsGenericType && SymbolEqualityComparer.Default.Equals(iface.OriginalDefinition, unboundInterface))
            {
                return iface;
            }

            if (!iface.IsGenericType && SymbolEqualityComparer.Default.Equals(iface, unboundInterface))
            {
                return iface;
            }
        }

        return null;
    }

    public static INamedTypeSymbol? FindGenericInterface(INamedTypeSymbol type, string metadataName, Compilation compilation)
    {
        INamedTypeSymbol? unbound = compilation.GetTypeByMetadataName(metadataName);
        if (unbound is null)
        {
            return null;
        }

        return type.AllInterfaces.FirstOrDefault(i =>
            i.IsGenericType && SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, unbound));
    }

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
}

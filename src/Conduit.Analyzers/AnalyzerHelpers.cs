using System.Linq;
using Microsoft.CodeAnalysis;

namespace Conduit.Analyzers;

internal static class AnalyzerHelpers
{
    public const string RequestMetadataName = "Conduit.Abstractions.IRequest`1";
    public const string RequestHandlerMetadataName = "Conduit.Abstractions.IRequestHandler`2";
    public const string NotificationHandlerMetadataName = "Conduit.Abstractions.INotificationHandler`1";

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
}

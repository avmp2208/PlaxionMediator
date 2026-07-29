using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Conduit.Analyzers;

/// <summary>
/// CONDUIT004: flags handler Handle methods missing a CancellationToken parameter.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MissingCancellationTokenAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(DiagnosticDescriptors.MissingCancellationToken);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        if (context.Symbol is not INamedTypeSymbol type || !AnalyzerHelpers.IsConcreteType(type))
        {
            return;
        }

        INamedTypeSymbol? requestHandler = context.Compilation.GetTypeByMetadataName(AnalyzerHelpers.RequestHandlerMetadataName);
        INamedTypeSymbol? notificationHandler = context.Compilation.GetTypeByMetadataName(AnalyzerHelpers.NotificationHandlerMetadataName);
        INamedTypeSymbol? cancellationToken = context.Compilation.GetTypeByMetadataName("System.Threading.CancellationToken");

        if (cancellationToken is null)
        {
            return;
        }

        bool isRequestHandler = requestHandler is not null
            && type.AllInterfaces.Any(i => i.IsGenericType && SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, requestHandler));
        bool isNotificationHandler = notificationHandler is not null
            && type.AllInterfaces.Any(i => i.IsGenericType && SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, notificationHandler));

        if (!isRequestHandler && !isNotificationHandler)
        {
            return;
        }

        foreach (IMethodSymbol method in type.GetMembers().OfType<IMethodSymbol>())
        {
            if (method.Name != "Handle" || method.MethodKind != MethodKind.Ordinary || method.IsStatic)
            {
                continue;
            }

            // Explicit interface implementations still named Handle in metadata sometimes; include public Handle.
            bool hasCancellation = method.Parameters.Any(p =>
                SymbolEqualityComparer.Default.Equals(p.Type, cancellationToken));

            if (hasCancellation)
            {
                continue;
            }

            // Only flag methods that look like handler entry points (at least one parameter: the request/notification).
            if (method.Parameters.Length == 0)
            {
                continue;
            }

            Location location = method.Locations.FirstOrDefault() ?? Location.None;
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.MissingCancellationToken,
                location,
                method.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
        }
    }
}

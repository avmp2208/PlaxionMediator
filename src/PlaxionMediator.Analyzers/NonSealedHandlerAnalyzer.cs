using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace PlaxionMediator.Analyzers;

/// <summary>
/// PlaxionMediator011: flags handler classes that are not sealed.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NonSealedHandlerAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(DiagnosticDescriptors.NonSealedHandler);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        if (context.Symbol is not INamedTypeSymbol type
            || type.TypeKind != TypeKind.Class
            || type.IsSealed
            || type.IsStatic
            || type.IsAbstract
            || !AnalyzerHelpers.IsConcreteType(type))
        {
            return;
        }

        if (!AnalyzerHelpers.IsHandlerType(type, context.Compilation))
        {
            return;
        }

        Location location = type.Locations.FirstOrDefault() ?? Location.None;
        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.NonSealedHandler,
            location,
            type.Name));
    }
}

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace PlaxionMediator.Analyzers;

/// <summary>
/// PlaxionMediator012: flags handler classes with more than one public constructor.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MultiplePublicConstructorsHandlerAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(DiagnosticDescriptors.MultiplePublicConstructorsInHandler);

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

        int publicCtorCount = type.Constructors
            .Count(c => c.DeclaredAccessibility == Accessibility.Public);

        if (publicCtorCount > 1)
        {
            Location location = type.Locations.FirstOrDefault() ?? Location.None;
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.MultiplePublicConstructorsInHandler,
                location,
                type.Name,
                publicCtorCount));
        }
    }
}

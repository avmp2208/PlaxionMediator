using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Conduit.Analyzers;

/// <summary>
/// CONDUIT003: flags request types that expose mutable public setters.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MutableRequestAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(DiagnosticDescriptors.MutableRequest);

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

        INamedTypeSymbol? requestUnbound = context.Compilation.GetTypeByMetadataName(AnalyzerHelpers.RequestMetadataName);
        if (!AnalyzerHelpers.ImplementsRequest(type, requestUnbound, out _))
        {
            return;
        }

        // Prefer sealed records; flag types with public set (not init) accessors.
        bool hasMutableSetter = type.GetMembers()
            .OfType<IPropertySymbol>()
            .Any(p => p.SetMethod is { DeclaredAccessibility: Accessibility.Public }
                      && !p.SetMethod.IsInitOnly
                      && !p.IsStatic);

        bool isSealedRecord = type is { IsRecord: true, IsSealed: true }
                              || type is { IsRecord: true, TypeKind: TypeKind.Struct, IsReadOnly: true };

        if (!hasMutableSetter && isSealedRecord)
        {
            return;
        }

        if (!hasMutableSetter && type.IsRecord)
        {
            // Non-sealed record without mutable setters: still encourage sealed, but only error on mutability for MVP.
            return;
        }

        if (!hasMutableSetter)
        {
            // Class/struct request without setters is acceptable for MVP mutability check.
            return;
        }

        Location location = type.Locations.FirstOrDefault() ?? Location.None;
        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.MutableRequest,
            location,
            type.Name));
    }
}

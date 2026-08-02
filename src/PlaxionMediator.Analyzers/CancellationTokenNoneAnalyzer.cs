using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace PlaxionMediator.Analyzers;

/// <summary>
/// PlaxionMediator032: flags CancellationToken.None inside handlers when an ambient token exists.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CancellationTokenNoneAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(DiagnosticDescriptors.CancellationTokenNoneUsage);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(AnalyzeFieldReference, OperationKind.FieldReference);
        context.RegisterOperationAction(AnalyzePropertyReference, OperationKind.PropertyReference);
    }

    private static void AnalyzeFieldReference(OperationAnalysisContext context)
    {
        if (context.Operation is not IFieldReferenceOperation field
            || field.Field.Name != "None"
            || field.Field.ContainingType?.Name != "CancellationToken")
        {
            return;
        }

        ReportIfApplicable(context, field.Syntax.GetLocation());
    }

    private static void AnalyzePropertyReference(OperationAnalysisContext context)
    {
        if (context.Operation is not IPropertyReferenceOperation property
            || property.Property.Name != "None"
            || property.Property.ContainingType?.Name != "CancellationToken")
        {
            return;
        }

        ReportIfApplicable(context, property.Syntax.GetLocation());
    }

    private static void ReportIfApplicable(OperationAnalysisContext context, Location location)
    {
        IMethodSymbol? method = GetContainingMethod(context.Operation);
        if (method is null || !AnalyzerHelpers.IsHandleMethod(method))
        {
            return;
        }

        if (method.ContainingType is null
            || !AnalyzerHelpers.IsHandlerOrBehaviorType(method.ContainingType, context.Compilation))
        {
            return;
        }

        INamedTypeSymbol? ctType = context.Compilation.GetTypeByMetadataName("System.Threading.CancellationToken");
        if (ctType is null || !AnalyzerHelpers.MethodHasCancellationToken(method, ctType))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.CancellationTokenNoneUsage,
            location,
            method.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
    }

    private static IMethodSymbol? GetContainingMethod(IOperation operation)
    {
        ISymbol? symbol = operation.SemanticModel?.GetEnclosingSymbol(operation.Syntax.SpanStart);
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
}

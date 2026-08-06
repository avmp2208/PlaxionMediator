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
        INamedTypeSymbol? ctType = context.Compilation.GetTypeByMetadataName("System.Threading.CancellationToken");
        if (ctType is null)
        {
            return;
        }

        (IMethodSymbol? method, IParameterSymbol? ambientToken) = ResolveHandleContext(context.Operation, ctType);
        if (method is null || !AnalyzerHelpers.IsHandleMethod(method))
        {
            return;
        }

        if (method.ContainingType is null
            || !AnalyzerHelpers.IsHandlerOrBehaviorType(method.ContainingType, context.Compilation))
        {
            return;
        }

        // No CancellationToken is actually reachable at this call site (e.g. a static local
        // function/lambda that cannot capture the ambient token), so CancellationToken.None is
        // the only option here — not a violation.
        if (ambientToken is null)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.CancellationTokenNoneUsage,
            location,
            method.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
    }

    /// <summary>
    /// Walks up from the call site through any local functions/lambdas to find the nearest
    /// ordinary (non-nested) containing method — typically the Handle method — while tracking
    /// which CancellationToken parameter is actually reachable as the "ambient" token at the call
    /// site. Once a *static* local function/lambda boundary is crossed without its own token, no
    /// further outer token is considered ambient, since static nested functions cannot capture
    /// outer locals/parameters at all.
    /// </summary>
    private static (IMethodSymbol? Containing, IParameterSymbol? AmbientToken) ResolveHandleContext(
        IOperation operation,
        INamedTypeSymbol ctType)
    {
        ISymbol? symbol = operation.SemanticModel?.GetEnclosingSymbol(operation.Syntax.SpanStart);
        IParameterSymbol? ambientToken = null;
        bool blockedByStaticBoundary = false;

        while (symbol is not null)
        {
            if (symbol is IMethodSymbol method)
            {
                if (ambientToken is null && !blockedByStaticBoundary)
                {
                    ambientToken = method.Parameters
                        .FirstOrDefault(p => SymbolEqualityComparer.Default.Equals(p.Type, ctType));
                }

                bool isNested = method.MethodKind is MethodKind.LocalFunction or MethodKind.LambdaMethod or MethodKind.AnonymousFunction;
                if (!isNested)
                {
                    return (method, ambientToken);
                }

                if (method.IsStatic)
                {
                    blockedByStaticBoundary = true;
                }
            }

            symbol = symbol.ContainingSymbol;
        }

        return (null, ambientToken);
    }
}

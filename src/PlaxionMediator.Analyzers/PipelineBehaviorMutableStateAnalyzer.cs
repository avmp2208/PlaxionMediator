using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace PlaxionMediator.Analyzers;

/// <summary>
/// PlaxionMediator023: flags pipeline behaviors that maintain mutable instance state.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PipelineBehaviorMutableStateAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(DiagnosticDescriptors.PipelineBehaviorCapturesMutableState);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(AnalyzeAssignment, OperationKind.SimpleAssignment);
        context.RegisterOperationAction(AnalyzeAssignment, OperationKind.CompoundAssignment);
        context.RegisterOperationAction(AnalyzeIncrementDecrement, OperationKind.Increment);
        context.RegisterOperationAction(AnalyzeIncrementDecrement, OperationKind.Decrement);
    }

    private static void AnalyzeAssignment(OperationAnalysisContext context)
    {
        if (context.Operation is IAssignmentOperation assignment)
        {
            AnalyzeTarget(context, assignment.Target);
        }
    }

    private static void AnalyzeIncrementDecrement(OperationAnalysisContext context)
    {
        if (context.Operation is IIncrementOrDecrementOperation incDec)
        {
            AnalyzeTarget(context, incDec.Target);
        }
    }

    private static void AnalyzeTarget(OperationAnalysisContext context, IOperation target)
    {
        ISymbol? symbol = null;
        if (target is IFieldReferenceOperation fieldRef)
        {
            symbol = fieldRef.Field;
        }
        else if (target is IPropertyReferenceOperation propRef)
        {
            symbol = propRef.Property;
        }

        if (symbol is null || symbol.IsStatic)
        {
            return;
        }

        // Only interested in instance members of the containing type
        IMethodSymbol? method = context.ContainingSymbol as IMethodSymbol;
        if (method is null || !AnalyzerHelpers.IsHandleMethod(method))
        {
            return;
        }

        if (!SymbolEqualityComparer.Default.Equals(symbol.ContainingType, method.ContainingType))
        {
            // Might be a base class member, still relevant if it's an instance member of the behavior
            if (!method.ContainingType.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, symbol.ContainingType))
                && !IsBaseType(method.ContainingType, symbol.ContainingType))
            {
                 return;
            }
        }

        if (method.ContainingType is null || !AnalyzerHelpers.IsBehaviorType(method.ContainingType, context.Compilation))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.PipelineBehaviorCapturesMutableState,
            target.Syntax.GetLocation(),
            method.ContainingType.Name,
            symbol.Name));
    }

    private static bool IsBaseType(INamedTypeSymbol type, ISymbol? potentialBase)
    {
        INamedTypeSymbol? current = type.BaseType;
        while (current != null)
        {
            if (SymbolEqualityComparer.Default.Equals(current, potentialBase))
            {
                return true;
            }
            current = current.BaseType;
        }
        return false;
    }
}

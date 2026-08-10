using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace PlaxionMediator.Analyzers;

/// <summary>
/// PlaxionMediator051: flags closures that capture local variables/parameters in handlers or behaviors.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ClosureCaptureInHandlerAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(DiagnosticDescriptors.ClosureCaptureInHandler);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(AnalyzeReference, OperationKind.LocalReference, OperationKind.ParameterReference);
    }

    private static void AnalyzeReference(OperationAnalysisContext context)
    {
        ISymbol? referencedSymbol = null;
        if (context.Operation is ILocalReferenceOperation localRef)
        {
            referencedSymbol = localRef.Local;
        }
        else if (context.Operation is IParameterReferenceOperation paramRef)
        {
            referencedSymbol = paramRef.Parameter;
        }

        if (referencedSymbol == null)
        {
            return;
        }

        // Check if this reference is inside an anonymous function or local function
        IOperation? current = context.Operation.Parent;
        bool isInsideFunction = false;
        while (current != null)
        {
            if (current.Kind is OperationKind.AnonymousFunction or OperationKind.LocalFunction)
            {
                isInsideFunction = true;
                break;
            }
            current = current.Parent;
        }

        if (!isInsideFunction)
        {
            return;
        }

        // We are inside a function. Now check if the referenced symbol is defined in the Handle method.
        // OperationAnalysisContext.ContainingSymbol for a reference inside a local function/lambda 
        // is typically that function/lambda's symbol.
        // We need to find the enclosing Handle method.
        
        IMethodSymbol? handleMethod = GetEnclosingHandleMethod(context.ContainingSymbol, context.Compilation);
        if (handleMethod == null)
        {
            return;
        }

        // If the symbol's containing symbol is the Handle method, it's a capture from the outer scope!
        if (SymbolEqualityComparer.Default.Equals(referencedSymbol.ContainingSymbol, handleMethod))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.ClosureCaptureInHandler,
                context.Operation.Syntax.GetLocation(),
                handleMethod.Name,
                referencedSymbol.Name));
        }
    }

    private static IMethodSymbol? GetEnclosingHandleMethod(ISymbol? symbol, Compilation compilation)
    {
        while (symbol != null)
        {
            if (symbol is IMethodSymbol method && AnalyzerHelpers.IsHandleMethod(method))
            {
                if (method.ContainingType != null && AnalyzerHelpers.IsHandlerOrBehaviorType(method.ContainingType, compilation))
                {
                    return method;
                }
            }
            symbol = symbol.ContainingSymbol;
        }
        return null;
    }
}

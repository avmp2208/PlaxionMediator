using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace PlaxionMediator.Analyzers;

/// <summary>
/// PlaxionMediator070: flags unawaited Task.Run calls inside handlers.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class FireAndForgetTaskRunAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(DiagnosticDescriptors.FireAndForgetTaskRun);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context)
    {
        if (context.Operation is not IInvocationOperation invocation)
        {
            return;
        }

        IMethodSymbol method = invocation.TargetMethod;
        if (method.Name != "Run" || method.ContainingType?.ToDisplayString() != "System.Threading.Tasks.Task")
        {
            return;
        }

        // Check if the result is discarded. 
        // In Roslyn, a discarded value in a statement usually has an IExpressionStatementOperation as parent.
        if (invocation.Parent is IExpressionStatementOperation)
        {
            IMethodSymbol? containingMethod = context.ContainingSymbol as IMethodSymbol;
            if (containingMethod is null || !AnalyzerHelpers.IsHandleMethod(containingMethod))
            {
                return;
            }

            if (containingMethod.ContainingType is null || !AnalyzerHelpers.IsHandlerType(containingMethod.ContainingType, context.Compilation))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.FireAndForgetTaskRun,
                invocation.Syntax.GetLocation(),
                containingMethod.Name));
        }
    }
}

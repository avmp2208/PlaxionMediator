using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace PlaxionMediator.Analyzers;

/// <summary>
/// PlaxionMediator041: flags handlers that Send their own request type via ISender.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class HandlerSelfSendAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(DiagnosticDescriptors.HandlerDependsOnISenderSelfType);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context)
    {
        if (context.Operation is not IInvocationOperation invocation
            || invocation.TargetMethod.Name != "Send")
        {
            return;
        }

        INamedTypeSymbol? sender = context.Compilation.GetTypeByMetadataName(AnalyzerHelpers.SenderMetadataName);
        if (sender is null
            || invocation.TargetMethod.ContainingType is null
            || !SymbolEqualityComparer.Default.Equals(invocation.TargetMethod.ContainingType, sender)
               && !invocation.TargetMethod.ContainingType.AllInterfaces.Contains(sender, SymbolEqualityComparer.Default))
        {
            // Also allow instance methods on types implementing ISender
            if (invocation.Instance?.Type is null
                || sender is null
                || !SymbolEqualityComparer.Default.Equals(invocation.Instance.Type, sender)
                   && invocation.Instance.Type.AllInterfaces.All(i => !SymbolEqualityComparer.Default.Equals(i, sender)))
            {
                return;
            }
        }

        IMethodSymbol? containing = GetContainingMethod(context.Operation);
        if (containing?.ContainingType is null || !AnalyzerHelpers.IsHandleMethod(containing))
        {
            return;
        }

        INamedTypeSymbol handlerType = containing.ContainingType;
        INamedTypeSymbol? requestHandler = context.Compilation.GetTypeByMetadataName(AnalyzerHelpers.RequestHandlerMetadataName);
        INamedTypeSymbol? handledRequest = AnalyzerHelpers.FindRequestHandlerInterface(handlerType, requestHandler)
            ?.TypeArguments.FirstOrDefault() as INamedTypeSymbol;
        if (handledRequest is null)
        {
            return;
        }

        if (invocation.Arguments.Length == 0)
        {
            return;
        }

        ITypeSymbol? sentType = invocation.Arguments[0].Value.Type;
        if (sentType is null || !SymbolEqualityComparer.Default.Equals(sentType, handledRequest))
        {
            // new Request(...) may be a conversion
            if (invocation.Arguments[0].Value is IConversionOperation conversion)
            {
                sentType = conversion.Operand.Type;
            }
            else if (invocation.Arguments[0].Value is IObjectCreationOperation creation)
            {
                sentType = creation.Type;
            }
        }

        if (sentType is null || !SymbolEqualityComparer.Default.Equals(sentType, handledRequest))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.HandlerDependsOnISenderSelfType,
            invocation.Syntax.GetLocation(),
            handlerType.Name,
            handledRequest.Name));
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

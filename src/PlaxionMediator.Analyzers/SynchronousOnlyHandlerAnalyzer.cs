using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace PlaxionMediator.Analyzers;

/// <summary>
/// PlaxionMediator081: flags handler methods with no await that could use ValueTask.FromResult.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SynchronousOnlyHandlerAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(DiagnosticDescriptors.SynchronousOnlyHandler);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
    }

    private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not MethodDeclarationSyntax methodDecl || methodDecl.Body is null)
        {
            return;
        }

        if (context.SemanticModel.GetDeclaredSymbol(methodDecl, context.CancellationToken) is not IMethodSymbol method)
        {
            return;
        }

        if (!AnalyzerHelpers.IsHandleMethod(method)
            || method.ContainingType is null
            || !AnalyzerHelpers.IsHandlerType(method.ContainingType, context.Compilation))
        {
            return;
        }

        // Skip async methods — they either await or should be flagged differently.
        if (method.IsAsync)
        {
            return;
        }

        bool hasAwait = methodDecl.DescendantNodes().Any(n => n is AwaitExpressionSyntax);
        if (hasAwait)
        {
            return;
        }

        // Only flag methods that return ValueTask/Task-like and build responses synchronously.
        string returnName = method.ReturnType.Name;
        if (returnName is not ("ValueTask" or "Task"))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.SynchronousOnlyHandler,
            methodDecl.Identifier.GetLocation(),
            method.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
    }
}

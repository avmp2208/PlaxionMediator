using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace PlaxionMediator.Analyzers;

/// <summary>
/// PlaxionMediator040: flags async void Handle methods on handlers/behaviors.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AsyncVoidHandlerAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(DiagnosticDescriptors.AsyncVoidHandler);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
    }

    private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not MethodDeclarationSyntax methodDecl)
        {
            return;
        }

        if (!methodDecl.Modifiers.Any(SyntaxKind.AsyncKeyword))
        {
            return;
        }

        if (methodDecl.ReturnType is not PredefinedTypeSyntax predefined
            || !predefined.Keyword.IsKind(SyntaxKind.VoidKeyword))
        {
            return;
        }

        if (context.SemanticModel.GetDeclaredSymbol(methodDecl, context.CancellationToken) is not IMethodSymbol method)
        {
            return;
        }

        if (!AnalyzerHelpers.IsHandleMethod(method))
        {
            return;
        }

        if (method.ContainingType is null
            || !AnalyzerHelpers.IsHandlerOrBehaviorType(method.ContainingType, context.Compilation))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.AsyncVoidHandler,
            methodDecl.Identifier.GetLocation(),
            method.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
    }
}

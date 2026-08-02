using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace PlaxionMediator.Analyzers;

/// <summary>
/// PlaxionMediator090: educational diagnostic for fail-fast throw patterns in notification handlers.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NotificationHandlerThrowsAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(DiagnosticDescriptors.NotificationHandlerThrowsWithoutAwaitingOthers);

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
            || !AnalyzerHelpers.IsNotificationHandlerType(method.ContainingType, context.Compilation))
        {
            return;
        }

        // Fail-fast pattern: catch block that only rethrows (throw; / throw ex;) suggesting the author
        // expects their exception to stop sibling notification handlers.
        foreach (CatchClauseSyntax catchClause in methodDecl.Body.DescendantNodes().OfType<CatchClauseSyntax>())
        {
            if (catchClause.Block.Statements.Count == 0)
            {
                continue;
            }

            bool onlyRethrow = catchClause.Block.Statements.All(s =>
                s is ThrowStatementSyntax throwStatement
                && (throwStatement.Expression is null
                    || throwStatement.Expression is IdentifierNameSyntax));

            if (!onlyRethrow)
            {
                continue;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.NotificationHandlerThrowsWithoutAwaitingOthers,
                catchClause.GetLocation(),
                method.ContainingType.Name));
            return;
        }
    }
}

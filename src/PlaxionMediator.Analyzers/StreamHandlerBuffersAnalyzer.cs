using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace PlaxionMediator.Analyzers;

/// <summary>
/// PlaxionMediator083: flags stream handlers that materialize the entire sequence before yielding.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class StreamHandlerBuffersAnalyzer : DiagnosticAnalyzer
{
    private static readonly ImmutableHashSet<string> MaterializingMethods = ImmutableHashSet.Create(
        "ToList", "ToArray", "ToListAsync", "ToArrayAsync", "ToDictionary", "ToHashSet");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(DiagnosticDescriptors.StreamHandlerBuffersSequence);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not InvocationExpressionSyntax invocation)
        {
            return;
        }

        string? name = invocation.Expression switch
        {
            MemberAccessExpressionSyntax member => member.Name.Identifier.Text,
            IdentifierNameSyntax id => id.Identifier.Text,
            _ => null,
        };

        if (name is null || !MaterializingMethods.Contains(name))
        {
            return;
        }

        ReportIfStreamHandler(context, invocation, name);
    }

    private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not ObjectCreationExpressionSyntax creation)
        {
            return;
        }

        string? typeName = creation.Type switch
        {
            IdentifierNameSyntax id => id.Identifier.Text,
            GenericNameSyntax g => g.Identifier.Text,
            _ => null,
        };

        if (typeName is not ("List" or "Dictionary" or "HashSet"))
        {
            return;
        }

        IMethodSymbol? method = AnalyzerHelpers.GetEnclosingMethod(context.SemanticModel, creation);
        if (method is null || !IsStreamHandle(method, context.Compilation))
        {
            return;
        }

        // Only flag if the method also contains yield return (buffer-then-yield pattern).
        if (method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(context.CancellationToken)
                is not MethodDeclarationSyntax methodDecl)
        {
            return;
        }

        bool hasYield = methodDecl.DescendantNodes().Any(n => n is YieldStatementSyntax);
        if (!hasYield)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.StreamHandlerBuffersSequence,
            creation.GetLocation(),
            method.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
            "new " + typeName));
    }

    private static void ReportIfStreamHandler(SyntaxNodeAnalysisContext context, SyntaxNode node, string display)
    {
        IMethodSymbol? method = AnalyzerHelpers.GetEnclosingMethod(context.SemanticModel, node);
        if (method is null || !IsStreamHandle(method, context.Compilation))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.StreamHandlerBuffersSequence,
            node.GetLocation(),
            method.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
            display));
    }

    private static bool IsStreamHandle(IMethodSymbol method, Compilation compilation)
    {
        if (!AnalyzerHelpers.IsHandleMethod(method) || method.ContainingType is null)
        {
            return false;
        }

        return AnalyzerHelpers.IsStreamHandlerType(method.ContainingType, compilation);
    }
}

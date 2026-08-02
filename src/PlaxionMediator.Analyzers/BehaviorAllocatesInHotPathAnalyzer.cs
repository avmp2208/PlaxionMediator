using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace PlaxionMediator.Analyzers;

/// <summary>
/// PlaxionMediator082: flags pipeline behaviors that allocate collections/closures per Handle call.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class BehaviorAllocatesInHotPathAnalyzer : DiagnosticAnalyzer
{
    private static readonly ImmutableHashSet<string> CollectionTypeNames = ImmutableHashSet.Create(
        "List", "Dictionary", "HashSet", "Queue", "Stack", "ConcurrentBag", "ConcurrentDictionary");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(DiagnosticDescriptors.BehaviorAllocatesInHotPath);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);
        context.RegisterSyntaxNodeAction(AnalyzeImplicitObjectCreation, SyntaxKind.ImplicitObjectCreationExpression);
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
            QualifiedNameSyntax q => AnalyzerHelpers.GetSimpleName(q.Right),
            _ => null,
        };

        ReportIfNeeded(context, creation, typeName);
    }

    private static void AnalyzeImplicitObjectCreation(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not ImplicitObjectCreationExpressionSyntax creation)
        {
            return;
        }

        ITypeSymbol? type = context.SemanticModel.GetTypeInfo(creation, context.CancellationToken).Type;
        ReportIfNeeded(context, creation, type?.Name);
    }

    private static void ReportIfNeeded(SyntaxNodeAnalysisContext context, SyntaxNode node, string? typeName)
    {
        if (typeName is null || !CollectionTypeNames.Contains(typeName))
        {
            return;
        }

        IMethodSymbol? method = AnalyzerHelpers.GetEnclosingMethod(context.SemanticModel, node);
        if (method is null || !AnalyzerHelpers.IsHandleMethod(method))
        {
            return;
        }

        if (method.ContainingType is null
            || !AnalyzerHelpers.IsBehaviorType(method.ContainingType, context.Compilation))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.BehaviorAllocatesInHotPath,
            node.GetLocation(),
            method.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
            typeName));
    }
}

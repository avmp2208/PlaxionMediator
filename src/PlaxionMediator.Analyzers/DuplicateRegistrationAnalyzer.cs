using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace PlaxionMediator.Analyzers;

/// <summary>
/// PlaxionMediator021: flags the same behavior type registered twice on a PipelineBuilder chain.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DuplicateRegistrationAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(DiagnosticDescriptors.DuplicateRegistration);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not InvocationExpressionSyntax invocation)
        {
            return;
        }

        // Only analyze the outermost chained call expression root once by requiring parent is not member access of Use.
        if (invocation.Parent is MemberAccessExpressionSyntax)
        {
            return;
        }

        List<(INamedTypeSymbol Type, Location Location)> uses = [];
        CollectUseCalls(invocation, context.SemanticModel, uses, context.CancellationToken);
        if (uses.Count < 2)
        {
            return;
        }

        HashSet<INamedTypeSymbol> seen = new(SymbolEqualityComparer.Default);
        foreach ((INamedTypeSymbol type, Location location) in uses)
        {
            if (!seen.Add(type))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.DuplicateRegistration,
                    location,
                    type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
            }
        }
    }

    private static void CollectUseCalls(
        ExpressionSyntax expression,
        SemanticModel model,
        List<(INamedTypeSymbol Type, Location Location)> uses,
        System.Threading.CancellationToken cancellationToken)
    {
        if (expression is not InvocationExpressionSyntax invocation)
        {
            return;
        }

        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            CollectUseCalls(memberAccess.Expression, model, uses, cancellationToken);

            if (memberAccess.Name is GenericNameSyntax generic
                && generic.Identifier.Text == "Use"
                && generic.TypeArgumentList.Arguments.Count == 1)
            {
                ITypeSymbol? receiverType = model.GetTypeInfo(memberAccess.Expression, cancellationToken).Type;
                bool isPipelineBuilder = receiverType?.ToDisplayString() == AnalyzerHelpers.PipelineBuilderMetadataName
                    || model.GetTypeInfo(invocation, cancellationToken).Type?.ToDisplayString() == AnalyzerHelpers.PipelineBuilderMetadataName;

                // Chained builder returns PipelineBuilder; walk chain regardless.
                TypeSyntax typeArgSyntax = generic.TypeArgumentList.Arguments[0];
                INamedTypeSymbol? type =
                    model.GetTypeInfo(typeArgSyntax, cancellationToken).Type as INamedTypeSymbol
                    ?? model.GetSymbolInfo(typeArgSyntax, cancellationToken).Symbol as INamedTypeSymbol;
                if (type is not null
                    && (IsPipelineBuilderChain(invocation, model, cancellationToken) || isPipelineBuilder))
                {
                    uses.Add((type, typeArgSyntax.GetLocation()));
                }
            }
        }
    }

    private static bool IsPipelineBuilderChain(
        InvocationExpressionSyntax invocation,
        SemanticModel model,
        System.Threading.CancellationToken cancellationToken)
    {
        ExpressionSyntax current = invocation;
        while (true)
        {
            ITypeSymbol? type = model.GetTypeInfo(current, cancellationToken).Type;
            if (type?.ToDisplayString() == AnalyzerHelpers.PipelineBuilderMetadataName)
            {
                return true;
            }

            if (current is InvocationExpressionSyntax inv
                && inv.Expression is MemberAccessExpressionSyntax ma)
            {
                current = ma.Expression;
                continue;
            }

            if (current is ObjectCreationExpressionSyntax creation)
            {
                ITypeSymbol? created = model.GetTypeInfo(creation, cancellationToken).Type;
                return created?.ToDisplayString() == AnalyzerHelpers.PipelineBuilderMetadataName;
            }

            if (current is IdentifierNameSyntax or MemberAccessExpressionSyntax)
            {
                ITypeSymbol? t = model.GetTypeInfo(current, cancellationToken).Type;
                return t?.ToDisplayString() == AnalyzerHelpers.PipelineBuilderMetadataName;
            }

            return false;
        }
    }
}

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace PlaxionMediator.Analyzers;

/// <summary>
/// PlaxionMediator020: flags PipelineBuilder.Use&lt;T&gt;() with a type that is not IPipelineBehavior.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class InvalidBehaviorRegistrationAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(DiagnosticDescriptors.InvalidBehaviorRegistration);

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

        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess
            || memberAccess.Name is not GenericNameSyntax genericName
            || genericName.Identifier.Text != "Use"
            || genericName.TypeArgumentList.Arguments.Count != 1)
        {
            return;
        }

        IMethodSymbol? method = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol as IMethodSymbol;
        if (method is null)
        {
            method = context.SemanticModel.GetSymbolInfo(memberAccess, context.CancellationToken).Symbol as IMethodSymbol;
        }

        if (method is null
            || method.ContainingType is null
            || method.ContainingType.ToDisplayString() != AnalyzerHelpers.PipelineBuilderMetadataName)
        {
            // Fallback: receiver type is PipelineBuilder
            ITypeSymbol? receiverType = context.SemanticModel.GetTypeInfo(memberAccess.Expression, context.CancellationToken).Type;
            if (receiverType?.ToDisplayString() != AnalyzerHelpers.PipelineBuilderMetadataName)
            {
                return;
            }
        }

        TypeSyntax typeArgSyntax = genericName.TypeArgumentList.Arguments[0];
        ITypeSymbol? typeArg = context.SemanticModel.GetTypeInfo(typeArgSyntax, context.CancellationToken).Type
                               ?? context.SemanticModel.GetSymbolInfo(typeArgSyntax, context.CancellationToken).Symbol as ITypeSymbol;
        if (typeArg is not INamedTypeSymbol named)
        {
            return;
        }

        INamedTypeSymbol? behaviorUnbound = context.Compilation.GetTypeByMetadataName(AnalyzerHelpers.PipelineBehaviorMetadataName);
        if (behaviorUnbound is null)
        {
            return;
        }

        bool implements = named.AllInterfaces.Any(i =>
            i.IsGenericType && SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, behaviorUnbound));

        // Open generic behavior definitions: check constructed from definition interfaces
        if (!implements && named.IsGenericType)
        {
            implements = named.OriginalDefinition.AllInterfaces.Any(i =>
                i.IsGenericType && SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, behaviorUnbound));
        }

        if (implements)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.InvalidBehaviorRegistration,
            typeArgSyntax.GetLocation(),
            named.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
    }
}

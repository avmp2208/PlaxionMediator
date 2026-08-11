using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace PlaxionMediator.Analyzers;

/// <summary>
/// PlaxionMediator024: flags PipelineExtensionBuilder.Use&lt;T&gt;() with a type that is not IPipelineExtension.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class InvalidExtensionRegistrationAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(DiagnosticDescriptors.InvalidExtensionRegistration);

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
            || method.ContainingType.ToDisplayString() != AnalyzerHelpers.PipelineExtensionBuilderMetadataName)
        {
            ITypeSymbol? receiverType = context.SemanticModel.GetTypeInfo(memberAccess.Expression, context.CancellationToken).Type;
            if (receiverType?.ToDisplayString() != AnalyzerHelpers.PipelineExtensionBuilderMetadataName)
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

        INamedTypeSymbol? extensionInterface = context.Compilation.GetTypeByMetadataName(AnalyzerHelpers.PipelineExtensionMetadataName);
        if (extensionInterface is null)
        {
            return;
        }

        bool implements = named.AllInterfaces.Any(i =>
            SymbolEqualityComparer.Default.Equals(i, extensionInterface)
            || SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, extensionInterface));

        if (!implements && named.IsGenericType)
        {
            implements = named.OriginalDefinition.AllInterfaces.Any(i =>
                SymbolEqualityComparer.Default.Equals(i, extensionInterface)
                || SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, extensionInterface));
        }

        // Generic constraint on Use<TExtension> already enforces IPipelineExtension at compile time
        // for well-typed code; still flag when the constraint is somehow bypassed or the type is wrong.
        if (implements)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.InvalidExtensionRegistration,
            typeArgSyntax.GetLocation(),
            named.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
    }
}

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace PlaxionMediator.Analyzers;

/// <summary>
/// PlaxionMediator005: flags MapPlaxionMediatorGet/Delete usages where TRequest has nothing bindable from route/query.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MissingRequestBindingAttributeAnalyzer : DiagnosticAnalyzer
{
    private static readonly ImmutableHashSet<string> TargetMethodNames = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "MapPlaxionMediatorGet",
        "MapPlaxionMediatorDelete");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(DiagnosticDescriptors.MissingRequestBindingAttribute);

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

        SymbolInfo symbolInfo = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken);
        IMethodSymbol? method = symbolInfo.Symbol as IMethodSymbol
            ?? symbolInfo.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault();

        if (method is null || !method.IsGenericMethod || method.TypeArguments.Length < 1)
        {
            return;
        }

        if (!TargetMethodNames.Contains(method.Name)
            && !TargetMethodNames.Contains(method.OriginalDefinition.Name))
        {
            return;
        }

        // Accept either the real MinimalApis extension or test stubs with the same method name.
        ITypeSymbol requestType = method.TypeArguments[0];
        if (requestType is not INamedTypeSymbol namedRequest || namedRequest.TypeKind == TypeKind.Error)
        {
            return;
        }

        if (HasBindableSurface(namedRequest))
        {
            return;
        }

        Location location = invocation.GetLocation();
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            location = memberAccess.Name.GetLocation();
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.MissingRequestBindingAttribute,
            location,
            namedRequest.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
            method.Name));
    }

    private static bool HasBindableSurface(INamedTypeSymbol type)
    {
        // Public instance properties (including record primary-constructor synthesized properties).
        bool hasPublicProperty = type.GetMembers()
            .OfType<IPropertySymbol>()
            .Any(p => p is { IsStatic: false, DeclaredAccessibility: Accessibility.Public }
                      && p.GetMethod is not null);

        if (hasPublicProperty)
        {
            return true;
        }

        // Public constructors with at least one parameter (non-record types / explicit ctors).
        bool hasPublicParameterizedCtor = type.InstanceConstructors.Any(ctor =>
            ctor is { DeclaredAccessibility: Accessibility.Public, Parameters.Length: > 0 }
            && !ctor.IsStatic);

        return hasPublicParameterizedCtor;
    }
}

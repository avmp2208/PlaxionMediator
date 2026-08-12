using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace PlaxionMediator.Analyzers;

/// <summary>
/// PlaxionMediator043: flags TransactionBehavior registered outer to RetryBehavior
/// (Retry inside Transaction). Safe ordering is Retry outer, Transaction inner.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class InvalidTransactionOrderingAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(DiagnosticDescriptors.InvalidTransactionOrdering);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            ConcurrentBag<(int Order, Location Location, string Kind)> registrations = new();
            int order = 0;

            start.RegisterSyntaxNodeAction(ctx =>
            {
                if (ctx.Node is not InvocationExpressionSyntax invocation)
                {
                    return;
                }

                string? methodName = GetMethodName(invocation);
                if (methodName is null)
                {
                    return;
                }

                // UsePlaxionMediatorRetryBehavior / UsePlaxionMediatorTransactionBehavior on options
                if (methodName == "UsePlaxionMediatorRetryBehavior")
                {
                    registrations.Add((System.Threading.Interlocked.Increment(ref order), invocation.GetLocation(), "Retry"));
                    return;
                }

                if (methodName == "UsePlaxionMediatorTransactionBehavior")
                {
                    registrations.Add((System.Threading.Interlocked.Increment(ref order), invocation.GetLocation(), "Transaction"));
                    return;
                }

                // GlobalBehaviors.Add(typeof(RetryBehavior<,>)) / typeof(TransactionBehavior<,>)
                if (methodName == "Add"
                    && invocation.ArgumentList.Arguments.Count == 1
                    && invocation.ArgumentList.Arguments[0].Expression is TypeOfExpressionSyntax typeOf)
                {
                    ITypeSymbol? t = ctx.SemanticModel.GetTypeInfo(typeOf.Type, ctx.CancellationToken).Type
                        ?? ctx.SemanticModel.GetSymbolInfo(typeOf.Type, ctx.CancellationToken).Symbol as ITypeSymbol;
                    string? kind = ClassifyBehavior(t);
                    if (kind is not null)
                    {
                        registrations.Add((System.Threading.Interlocked.Increment(ref order), typeOf.GetLocation(), kind));
                    }

                    return;
                }

                // PipelineBuilder.Use<RetryBehavior<...>>() chained — collect order from chain root
                if (methodName == "Use"
                    && invocation.Expression is MemberAccessExpressionSyntax
                    {
                        Name: GenericNameSyntax { TypeArgumentList.Arguments.Count: 1 } generic
                    }
                    && invocation.Parent is not MemberAccessExpressionSyntax)
                {
                    List<(string Kind, Location Location)> chain = [];
                    CollectUseChain(invocation, ctx.SemanticModel, chain, ctx.CancellationToken);
                    foreach ((string kind, Location location) in chain)
                    {
                        registrations.Add((System.Threading.Interlocked.Increment(ref order), location, kind));
                    }
                }
            }, SyntaxKind.InvocationExpression);

            start.RegisterCompilationEndAction(end =>
            {
                List<(int Order, Location Location, string Kind)> ordered = registrations
                    .OrderBy(r => r.Order)
                    .ToList();

                int? retryIndex = null;
                int? transactionIndex = null;
                Location? transactionLocation = null;

                for (int i = 0; i < ordered.Count; i++)
                {
                    if (ordered[i].Kind == "Retry" && retryIndex is null)
                    {
                        retryIndex = i;
                    }
                    else if (ordered[i].Kind == "Transaction" && transactionIndex is null)
                    {
                        transactionIndex = i;
                        transactionLocation = ordered[i].Location;
                    }
                }

                // Unsafe when Transaction is registered before Retry (Transaction outer, Retry inner).
                if (retryIndex is int r
                    && transactionIndex is int t
                    && t < r
                    && transactionLocation is not null)
                {
                    end.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.InvalidTransactionOrdering,
                        transactionLocation));
                }
            });
        });
    }

    private static void CollectUseChain(
        ExpressionSyntax expression,
        SemanticModel model,
        List<(string Kind, Location Location)> chain,
        System.Threading.CancellationToken cancellationToken)
    {
        if (expression is not InvocationExpressionSyntax invocation)
        {
            return;
        }

        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            CollectUseChain(memberAccess.Expression, model, chain, cancellationToken);

            if (memberAccess.Name is GenericNameSyntax generic
                && generic.Identifier.Text == "Use"
                && generic.TypeArgumentList.Arguments.Count == 1)
            {
                TypeSyntax typeArgSyntax = generic.TypeArgumentList.Arguments[0];
                ITypeSymbol? type = model.GetTypeInfo(typeArgSyntax, cancellationToken).Type
                    ?? model.GetSymbolInfo(typeArgSyntax, cancellationToken).Symbol as ITypeSymbol;
                string? kind = ClassifyBehavior(type);
                if (kind is not null)
                {
                    chain.Add((kind, typeArgSyntax.GetLocation()));
                }
            }
        }
    }

    private static string? ClassifyBehavior(ITypeSymbol? type)
    {
        if (type is not INamedTypeSymbol named)
        {
            return null;
        }

        INamedTypeSymbol definition = named.IsGenericType ? named.OriginalDefinition : named;
        string display = definition.ToDisplayString();
        if (display == AnalyzerHelpers.RetryBehaviorMetadataName || definition.Name == "RetryBehavior")
        {
            return "Retry";
        }

        if (display == AnalyzerHelpers.TransactionBehaviorMetadataName || definition.Name == "TransactionBehavior")
        {
            return "Transaction";
        }

        return null;
    }

    private static string? GetMethodName(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression switch
        {
            MemberAccessExpressionSyntax member => member.Name switch
            {
                GenericNameSyntax g => g.Identifier.Text,
                IdentifierNameSyntax id => id.Identifier.Text,
                _ => null,
            },
            IdentifierNameSyntax id => id.Identifier.Text,
            GenericNameSyntax g => g.Identifier.Text,
            _ => null,
        };
    }
}

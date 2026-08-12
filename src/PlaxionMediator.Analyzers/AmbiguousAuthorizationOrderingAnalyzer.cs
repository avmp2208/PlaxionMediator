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
/// PlaxionMediator048: flags AuthorizationBehavior registered inner to RetryBehavior or TransactionBehavior.
/// Recommended ordering is Authorization outer to both Retry and Transaction.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AmbiguousAuthorizationOrderingAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(DiagnosticDescriptors.AmbiguousAuthorizationOrdering);

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

                if (methodName == "UsePlaxionMediatorAuthorizationBehavior")
                {
                    registrations.Add((System.Threading.Interlocked.Increment(ref order), invocation.GetLocation(), "Authorization"));
                    return;
                }

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

                // GlobalBehaviors.Add(typeof(...))
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

                // PipelineBuilder.Use<...>() chained — collect order from chain root
                if (methodName == "Use"
                    && invocation.Expression is MemberAccessExpressionSyntax
                    {
                        Name: GenericNameSyntax { TypeArgumentList.Arguments.Count: 1 }
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

                int? authorizationIndex = null;
                int? retryIndex = null;
                int? transactionIndex = null;
                Location? authorizationLocation = null;

                for (int i = 0; i < ordered.Count; i++)
                {
                    switch (ordered[i].Kind)
                    {
                        case "Authorization" when authorizationIndex is null:
                            authorizationIndex = i;
                            authorizationLocation = ordered[i].Location;
                            break;
                        case "Retry" when retryIndex is null:
                            retryIndex = i;
                            break;
                        case "Transaction" when transactionIndex is null:
                            transactionIndex = i;
                            break;
                    }
                }

                if (authorizationIndex is not int auth || authorizationLocation is null)
                {
                    return;
                }

                // Unsafe when Authorization is registered after (inner to) Retry or Transaction.
                List<string> offenders = [];
                if (retryIndex is int r && auth > r)
                {
                    offenders.Add("RetryBehavior");
                }

                if (transactionIndex is int t && auth > t)
                {
                    offenders.Add("TransactionBehavior");
                }

                if (offenders.Count == 0)
                {
                    return;
                }

                end.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.AmbiguousAuthorizationOrdering,
                    authorizationLocation,
                    string.Join(" and ", offenders)));
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
        if (display == AnalyzerHelpers.AuthorizationBehaviorMetadataName || definition.Name == "AuthorizationBehavior")
        {
            return "Authorization";
        }

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

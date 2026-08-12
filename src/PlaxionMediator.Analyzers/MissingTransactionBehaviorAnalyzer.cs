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
/// PlaxionMediator042: flags ITransactionalRequest types when TransactionBehavior is not registered.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MissingTransactionBehaviorAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(DiagnosticDescriptors.MissingTransactionBehavior);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            INamedTypeSymbol? transactional = start.Compilation.GetTypeByMetadataName(AnalyzerHelpers.TransactionalRequestMetadataName);
            if (transactional is null)
            {
                return;
            }

            ConcurrentBag<byte> transactionBehaviorRegistered = new();

            start.RegisterSyntaxNodeAction(ctx =>
            {
                if (IsTransactionBehaviorRegistration(ctx.Node, ctx.SemanticModel, ctx.CancellationToken))
                {
                    transactionBehaviorRegistered.Add(0);
                }
            }, SyntaxKind.InvocationExpression);

            start.RegisterCompilationEndAction(end =>
            {
                if (!transactionBehaviorRegistered.IsEmpty)
                {
                    return;
                }

                foreach (INamedTypeSymbol type in GetAllTypes(end.Compilation.Assembly.GlobalNamespace))
                {
                    if (!AnalyzerHelpers.IsConcreteType(type)
                        || !type.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, transactional)))
                    {
                        continue;
                    }

                    Location location = type.Locations.FirstOrDefault() ?? Location.None;
                    end.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.MissingTransactionBehavior,
                        location,
                        type.Name));
                }
            });
        });
    }

    internal static bool IsTransactionBehaviorRegistration(
        SyntaxNode node,
        SemanticModel model,
        System.Threading.CancellationToken cancellationToken)
    {
        if (node is not InvocationExpressionSyntax invocation)
        {
            return false;
        }

        string? methodName = GetMethodName(invocation);
        if (methodName is "UsePlaxionMediatorTransactionBehavior"
            or "AddPlaxionMediatorTransactions"
            or "AddPlaxionMediatorTransactionsEntityFrameworkCore")
        {
            return true;
        }

        // PipelineBuilder.Use<TransactionBehavior<...>>() / Use<TransactionBehavior<,>>()
        if (methodName == "Use"
            && invocation.Expression is MemberAccessExpressionSyntax
            {
                Name: GenericNameSyntax { TypeArgumentList.Arguments.Count: 1 } generic
            })
        {
            ITypeSymbol? typeArg = model.GetTypeInfo(generic.TypeArgumentList.Arguments[0], cancellationToken).Type
                ?? model.GetSymbolInfo(generic.TypeArgumentList.Arguments[0], cancellationToken).Symbol as ITypeSymbol;
            if (IsTransactionBehaviorType(typeArg))
            {
                return true;
            }
        }

        // GlobalBehaviors.Add(typeof(TransactionBehavior<,>))
        if (methodName == "Add"
            && invocation.ArgumentList.Arguments.Count == 1
            && invocation.ArgumentList.Arguments[0].Expression is TypeOfExpressionSyntax typeOf)
        {
            ITypeSymbol? t = model.GetTypeInfo(typeOf.Type, cancellationToken).Type
                ?? model.GetSymbolInfo(typeOf.Type, cancellationToken).Symbol as ITypeSymbol;
            if (IsTransactionBehaviorType(t))
            {
                return true;
            }
        }

        // services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>))
        if (methodName is "AddTransient" or "AddScoped" or "AddSingleton" or "TryAddEnumerable")
        {
            foreach (ArgumentSyntax arg in invocation.ArgumentList.Arguments)
            {
                if (arg.Expression is TypeOfExpressionSyntax to)
                {
                    ITypeSymbol? t = model.GetTypeInfo(to.Type, cancellationToken).Type
                        ?? model.GetSymbolInfo(to.Type, cancellationToken).Symbol as ITypeSymbol;
                    if (IsTransactionBehaviorType(t))
                    {
                        return true;
                    }
                }
            }

            if (invocation.Expression is MemberAccessExpressionSyntax
                {
                    Name: GenericNameSyntax { TypeArgumentList.Arguments: { Count: > 0 } args }
                })
            {
                TypeSyntax impl = args.Count >= 2 ? args[1] : args[0];
                ITypeSymbol? t = model.GetTypeInfo(impl, cancellationToken).Type
                    ?? model.GetSymbolInfo(impl, cancellationToken).Symbol as ITypeSymbol;
                if (IsTransactionBehaviorType(t))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsTransactionBehaviorType(ITypeSymbol? type)
    {
        if (type is not INamedTypeSymbol named)
        {
            return false;
        }

        INamedTypeSymbol definition = named.IsGenericType ? named.OriginalDefinition : named;
        return definition.ToDisplayString() == AnalyzerHelpers.TransactionBehaviorMetadataName
               || definition.Name == "TransactionBehavior";
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

    private static IEnumerable<INamedTypeSymbol> GetAllTypes(INamespaceSymbol ns)
    {
        foreach (INamedTypeSymbol type in ns.GetTypeMembers())
        {
            yield return type;
            foreach (INamedTypeSymbol nested in GetNested(type))
            {
                yield return nested;
            }
        }

        foreach (INamespaceSymbol child in ns.GetNamespaceMembers())
        {
            foreach (INamedTypeSymbol type in GetAllTypes(child))
            {
                yield return type;
            }
        }
    }

    private static IEnumerable<INamedTypeSymbol> GetNested(INamedTypeSymbol type)
    {
        foreach (INamedTypeSymbol nested in type.GetTypeMembers())
        {
            yield return nested;
            foreach (INamedTypeSymbol deeper in GetNested(nested))
            {
                yield return deeper;
            }
        }
    }
}

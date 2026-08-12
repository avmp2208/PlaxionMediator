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
/// PlaxionMediator044: flags multiple ITransactionManager implementations registered without keyed resolution.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AmbiguousTransactionManagerAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(DiagnosticDescriptors.AmbiguousTransactionManager);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            INamedTypeSymbol? managerInterface = start.Compilation.GetTypeByMetadataName(AnalyzerHelpers.TransactionManagerMetadataName);
            if (managerInterface is null)
            {
                return;
            }

            ConcurrentBag<(string ImplName, Location Location, bool IsKeyed)> registrations = new();

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

                bool isKeyed = methodName.StartsWith("AddKeyed", System.StringComparison.Ordinal)
                    || HasKeyedServiceKeyArgument(invocation);

                // AddPlaxionMediatorTransactionManager<T>() / AddPlaxionMediatorTransactionsEntityFrameworkCore<T>()
                if (methodName is "AddPlaxionMediatorTransactionManager"
                    or "AddPlaxionMediatorTransactionsEntityFrameworkCore")
                {
                    string implName = methodName;
                    if (invocation.Expression is MemberAccessExpressionSyntax
                        {
                            Name: GenericNameSyntax { TypeArgumentList.Arguments.Count: > 0 } g
                        })
                    {
                        implName = g.TypeArgumentList.Arguments[0].ToString();
                    }

                    registrations.Add((implName, invocation.GetLocation(), isKeyed));
                    return;
                }

                if (methodName is not ("AddSingleton" or "AddScoped" or "AddTransient"
                    or "AddKeyedSingleton" or "AddKeyedScoped" or "AddKeyedTransient"
                    or "TryAddSingleton" or "TryAddScoped" or "TryAddTransient"))
                {
                    return;
                }

                // AddScoped<ITransactionManager, TImpl>()
                if (invocation.Expression is MemberAccessExpressionSyntax
                    {
                        Name: GenericNameSyntax { TypeArgumentList.Arguments: { Count: > 0 } args }
                    })
                {
                    ITypeSymbol? serviceType = ctx.SemanticModel.GetTypeInfo(args[0], ctx.CancellationToken).Type
                        ?? ctx.SemanticModel.GetSymbolInfo(args[0], ctx.CancellationToken).Symbol as ITypeSymbol;
                    if (serviceType is not null
                        && SymbolEqualityComparer.Default.Equals(serviceType, managerInterface))
                    {
                        string impl = args.Count >= 2 ? args[1].ToString() : args[0].ToString();
                        registrations.Add((impl, args[0].GetLocation(), isKeyed));
                        return;
                    }

                    // AddScoped<TImpl>() where TImpl : ITransactionManager
                    if (args.Count == 1
                        && serviceType is INamedTypeSymbol named
                        && named.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, managerInterface)))
                    {
                        registrations.Add((named.Name, args[0].GetLocation(), isKeyed));
                        return;
                    }
                }

                // AddScoped(typeof(ITransactionManager), typeof(TImpl))
                if (invocation.ArgumentList.Arguments.Count >= 1
                    && invocation.ArgumentList.Arguments[0].Expression is TypeOfExpressionSyntax serviceTypeOf)
                {
                    ITypeSymbol? serviceType = ctx.SemanticModel.GetTypeInfo(serviceTypeOf.Type, ctx.CancellationToken).Type
                        ?? ctx.SemanticModel.GetSymbolInfo(serviceTypeOf.Type, ctx.CancellationToken).Symbol as ITypeSymbol;
                    if (serviceType is not null
                        && SymbolEqualityComparer.Default.Equals(serviceType, managerInterface))
                    {
                        string impl = invocation.ArgumentList.Arguments.Count >= 2
                            ? invocation.ArgumentList.Arguments[1].ToString()
                            : serviceTypeOf.Type.ToString();
                        registrations.Add((impl, serviceTypeOf.GetLocation(), isKeyed));
                    }
                }
            }, SyntaxKind.InvocationExpression);

            start.RegisterCompilationEndAction(end =>
            {
                List<(string ImplName, Location Location, bool IsKeyed)> list = registrations.ToList();
                if (list.Count < 2)
                {
                    return;
                }

                // Keyed registrations are an explicit resolution strategy — do not flag.
                if (list.All(r => r.IsKeyed))
                {
                    return;
                }

                List<(string ImplName, Location Location, bool IsKeyed)> nonKeyed = list.Where(r => !r.IsKeyed).ToList();
                if (nonKeyed.Count < 2)
                {
                    return;
                }

                // Distinct implementation names
                List<string> distinct = nonKeyed.Select(r => r.ImplName).Distinct().ToList();
                if (distinct.Count < 2)
                {
                    return;
                }

                Location location = nonKeyed[1].Location;
                end.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.AmbiguousTransactionManager,
                    location,
                    string.Join(", ", distinct)));
            });
        });
    }

    private static bool HasKeyedServiceKeyArgument(InvocationExpressionSyntax invocation)
    {
        // Heuristic: AddKeyed* or first arg looks like a service key literal/nameof when using non-generic overloads.
        string? name = GetMethodName(invocation);
        return name is not null && name.StartsWith("AddKeyed", System.StringComparison.Ordinal);
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

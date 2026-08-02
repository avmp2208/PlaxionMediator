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
/// PlaxionMediator022: flags Singleton handlers/behaviors that capture Scoped/Transient dependencies.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class IncorrectLifetimeAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(DiagnosticDescriptors.IncorrectLifetime);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            ConcurrentDictionary<INamedTypeSymbol, string> lifetimes =
                new(SymbolEqualityComparer.Default);

            start.RegisterSyntaxNodeAction(ctx => AnalyzeRegistration(ctx, lifetimes), SyntaxKind.InvocationExpression);

            start.RegisterCompilationEndAction(end =>
            {
                foreach (KeyValuePair<INamedTypeSymbol, string> pair in lifetimes)
                {
                    if (pair.Value != "Singleton")
                    {
                        continue;
                    }

                    INamedTypeSymbol type = pair.Key;
                    if (!AnalyzerHelpers.IsHandlerOrBehaviorType(type, end.Compilation))
                    {
                        continue;
                    }

                    IMethodSymbol? ctor = type.InstanceConstructors
                        .OrderByDescending(c => c.Parameters.Length)
                        .FirstOrDefault(c => !c.IsStatic);
                    if (ctor is null)
                    {
                        continue;
                    }

                    foreach (IParameterSymbol parameter in ctor.Parameters)
                    {
                        if (parameter.Type is not INamedTypeSymbol dependencyType)
                        {
                            continue;
                        }

                        if (!lifetimes.TryGetValue(dependencyType, out string? depLifetime))
                        {
                            continue;
                        }

                        if (depLifetime is not ("Scoped" or "Transient"))
                        {
                            continue;
                        }

                        Location location = type.Locations.FirstOrDefault() ?? Location.None;
                        end.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.IncorrectLifetime,
                            location,
                            type.Name,
                            dependencyType.Name,
                            depLifetime));
                    }
                }
            });
        });
    }

    private static void AnalyzeRegistration(
        SyntaxNodeAnalysisContext context,
        ConcurrentDictionary<INamedTypeSymbol, string> lifetimes)
    {
        if (context.Node is not InvocationExpressionSyntax invocation)
        {
            return;
        }

        string? methodName = GetExtensionMethodName(invocation);
        if (methodName is not ("AddSingleton" or "AddScoped" or "AddTransient"))
        {
            return;
        }

        string lifetime = methodName switch
        {
            "AddSingleton" => "Singleton",
            "AddScoped" => "Scoped",
            _ => "Transient",
        };

        foreach (INamedTypeSymbol type in GetRegisteredTypes(invocation, context.SemanticModel, context.CancellationToken))
        {
            lifetimes[type] = lifetime;
        }
    }

    private static string? GetExtensionMethodName(InvocationExpressionSyntax invocation)
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

    private static IEnumerable<INamedTypeSymbol> GetRegisteredTypes(
        InvocationExpressionSyntax invocation,
        SemanticModel model,
        System.Threading.CancellationToken cancellationToken)
    {
        if (invocation.Expression is MemberAccessExpressionSyntax
            {
                Name: GenericNameSyntax { TypeArgumentList.Arguments: { Count: > 0 } args }
            })
        {
            // AddSingleton<T>() or AddSingleton<TService, TImpl>()
            TypeSyntax implSyntax = args.Count == 2 ? args[1] : args[0];
            INamedTypeSymbol? type =
                model.GetTypeInfo(implSyntax, cancellationToken).Type as INamedTypeSymbol
                ?? model.GetSymbolInfo(implSyntax, cancellationToken).Symbol as INamedTypeSymbol;
            if (type is not null)
            {
                yield return type;
            }

            yield break;
        }

        // AddSingleton(typeof(T)) / AddSingleton(typeof(TService), typeof(TImpl))
        foreach (ArgumentSyntax argument in invocation.ArgumentList.Arguments)
        {
            if (argument.Expression is TypeOfExpressionSyntax typeOf
                && (model.GetTypeInfo(typeOf.Type, cancellationToken).Type is INamedTypeSymbol t
                    || model.GetSymbolInfo(typeOf.Type, cancellationToken).Symbol is INamedTypeSymbol))
            {
                INamedTypeSymbol symbol = model.GetTypeInfo(typeOf.Type, cancellationToken).Type as INamedTypeSymbol
                    ?? (INamedTypeSymbol)model.GetSymbolInfo(typeOf.Type, cancellationToken).Symbol!;
                yield return symbol;
            }
        }
    }
}

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
/// PlaxionMediator047: flags IRequestAuthorization&lt;T&gt; implementations when AuthorizationBehavior is not registered.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UnregisteredAuthorizationBehaviorAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(DiagnosticDescriptors.UnregisteredAuthorizationBehavior);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            INamedTypeSymbol? requestAuthorization = start.Compilation.GetTypeByMetadataName(
                AnalyzerHelpers.RequestAuthorizationInterfaceMetadataName);
            if (requestAuthorization is null)
            {
                return;
            }

            ConcurrentBag<byte> authorizationBehaviorRegistered = new();

            start.RegisterSyntaxNodeAction(ctx =>
            {
                if (IsAuthorizationBehaviorRegistration(ctx.Node, ctx.SemanticModel, ctx.CancellationToken))
                {
                    authorizationBehaviorRegistered.Add(0);
                }
            }, SyntaxKind.InvocationExpression);

            start.RegisterCompilationEndAction(end =>
            {
                if (!authorizationBehaviorRegistered.IsEmpty)
                {
                    return;
                }

                foreach (INamedTypeSymbol type in GetAllTypes(end.Compilation.Assembly.GlobalNamespace))
                {
                    if (!AnalyzerHelpers.IsConcreteType(type)
                        || !ImplementsRequestAuthorization(type, requestAuthorization))
                    {
                        continue;
                    }

                    Location location = type.Locations.FirstOrDefault() ?? Location.None;
                    end.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.UnregisteredAuthorizationBehavior,
                        location,
                        type.Name));
                }
            });
        });
    }

    internal static bool IsAuthorizationBehaviorRegistration(
        SyntaxNode node,
        SemanticModel model,
        System.Threading.CancellationToken cancellationToken)
    {
        if (node is not InvocationExpressionSyntax invocation)
        {
            return false;
        }

        string? methodName = GetMethodName(invocation);
        if (methodName is "UsePlaxionMediatorAuthorizationBehavior"
            or "AddPlaxionMediatorAuthorization")
        {
            // Non-generic AddPlaxionMediatorAuthorization registers the behavior.
            // Generic AddPlaxionMediatorAuthorization<TRequest,TCheck> only registers a check —
            // still accept the method name as intentional authorization setup (mirrors transaction package helpers).
            if (methodName == "AddPlaxionMediatorAuthorization"
                && invocation.Expression is MemberAccessExpressionSyntax
                {
                    Name: GenericNameSyntax { TypeArgumentList.Arguments.Count: 2 }
                })
            {
                // Check-only registration does not enable AuthorizationBehavior.
                return false;
            }

            return true;
        }

        // PipelineBuilder.Use<AuthorizationBehavior<...>>() / Use<AuthorizationBehavior<,>>()
        if (methodName == "Use"
            && invocation.Expression is MemberAccessExpressionSyntax
            {
                Name: GenericNameSyntax { TypeArgumentList.Arguments.Count: 1 } generic
            })
        {
            ITypeSymbol? typeArg = model.GetTypeInfo(generic.TypeArgumentList.Arguments[0], cancellationToken).Type
                ?? model.GetSymbolInfo(generic.TypeArgumentList.Arguments[0], cancellationToken).Symbol as ITypeSymbol;
            if (IsAuthorizationBehaviorType(typeArg))
            {
                return true;
            }
        }

        // GlobalBehaviors.Add(typeof(AuthorizationBehavior<,>))
        if (methodName == "Add"
            && invocation.ArgumentList.Arguments.Count == 1
            && invocation.ArgumentList.Arguments[0].Expression is TypeOfExpressionSyntax typeOf)
        {
            ITypeSymbol? t = model.GetTypeInfo(typeOf.Type, cancellationToken).Type
                ?? model.GetSymbolInfo(typeOf.Type, cancellationToken).Symbol as ITypeSymbol;
            if (IsAuthorizationBehaviorType(t))
            {
                return true;
            }
        }

        // services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>))
        if (methodName is "AddTransient" or "AddScoped" or "AddSingleton" or "TryAddEnumerable")
        {
            foreach (ArgumentSyntax arg in invocation.ArgumentList.Arguments)
            {
                if (arg.Expression is TypeOfExpressionSyntax to)
                {
                    ITypeSymbol? t = model.GetTypeInfo(to.Type, cancellationToken).Type
                        ?? model.GetSymbolInfo(to.Type, cancellationToken).Symbol as ITypeSymbol;
                    if (IsAuthorizationBehaviorType(t))
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
                if (IsAuthorizationBehaviorType(t))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool ImplementsRequestAuthorization(INamedTypeSymbol type, INamedTypeSymbol unbound)
    {
        return type.AllInterfaces.Any(i =>
            i.IsGenericType
            && i.TypeArguments.Length == 1
            && SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, unbound));
    }

    private static bool IsAuthorizationBehaviorType(ITypeSymbol? type)
    {
        if (type is not INamedTypeSymbol named)
        {
            return false;
        }

        INamedTypeSymbol definition = named.IsGenericType ? named.OriginalDefinition : named;
        return definition.ToDisplayString() == AnalyzerHelpers.AuthorizationBehaviorMetadataName
               || definition.Name == "AuthorizationBehavior";
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

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

                        // Only flag dependencies that are actually retained as instance state (captured).
                        // A constructor parameter that is merely used inside the constructor body (or not
                        // used at all) does not create a long-lived reference and cannot leak a
                        // shorter-lived dependency into a Singleton's lifetime.
                        if (!IsParameterCaptured(end.Compilation, type, ctor, parameter))
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

    /// <summary>
    /// Determines whether a constructor parameter is actually retained as instance state
    /// (assigned to a field/property for a regular constructor, or referenced elsewhere in the
    /// type for a primary constructor). Handles partial classes by inspecting every declared
    /// part of the type, and primary constructors by treating any reference outside the
    /// parameter list as a capture, since the compiler only synthesizes a backing field for
    /// primary constructor parameters that are actually used outside the constructor.
    /// </summary>
    private static bool IsParameterCaptured(Compilation compilation, INamedTypeSymbol type, IMethodSymbol ctor, IParameterSymbol parameter)
    {
        foreach (SyntaxReference syntaxRef in ctor.DeclaringSyntaxReferences)
        {
            if (syntaxRef.GetSyntax() is ConstructorDeclarationSyntax ctorDecl)
            {
#pragma warning disable RS1030 // No cached SemanticModel is available for arbitrary declaring syntax trees at compilation-end time.
                SemanticModel model = compilation.GetSemanticModel(ctorDecl.SyntaxTree);
#pragma warning restore RS1030
                SyntaxNode? body = (SyntaxNode?)ctorDecl.Body ?? ctorDecl.ExpressionBody?.Expression;
                if (body is null)
                {
                    continue;
                }

                foreach (AssignmentExpressionSyntax assignment in body.DescendantNodesAndSelf().OfType<AssignmentExpressionSyntax>())
                {
                    if (assignment.Right is not IdentifierNameSyntax rightId
                        || rightId.Identifier.Text != parameter.Name)
                    {
                        continue;
                    }

                    ISymbol? rightSymbol = model.GetSymbolInfo(rightId).Symbol;
                    if (!SymbolEqualityComparer.Default.Equals(rightSymbol, parameter))
                    {
                        continue;
                    }

                    ISymbol? leftSymbol = model.GetSymbolInfo(assignment.Left).Symbol;
                    if (leftSymbol is IFieldSymbol or IPropertySymbol)
                    {
                        return true;
                    }
                }

                continue;
            }

            // Primary constructor: the syntax reference points at the type declaration itself.
            // Search every partial declaration part for a reference to the parameter outside its
            // own parameter list — such a reference forces the compiler to synthesize a
            // capturing field.
            if (syntaxRef.GetSyntax() is TypeDeclarationSyntax)
            {
                foreach (SyntaxReference typeRef in type.DeclaringSyntaxReferences)
                {
                    if (typeRef.GetSyntax() is not TypeDeclarationSyntax typeDecl)
                    {
                        continue;
                    }

#pragma warning disable RS1030 // No cached SemanticModel is available for arbitrary declaring syntax trees at compilation-end time.
                    SemanticModel model = compilation.GetSemanticModel(typeDecl.SyntaxTree);
#pragma warning restore RS1030
                    foreach (IdentifierNameSyntax idName in typeDecl.DescendantNodes().OfType<IdentifierNameSyntax>())
                    {
                        if (idName.Identifier.Text != parameter.Name)
                        {
                            continue;
                        }

                        if (typeDecl.ParameterList is { } parameterList && parameterList.Span.Contains(idName.Span))
                        {
                            continue;
                        }

                        ISymbol? symbol = model.GetSymbolInfo(idName).Symbol;
                        if (SymbolEqualityComparer.Default.Equals(symbol, parameter))
                        {
                            return true;
                        }
                    }
                }
            }
        }

        return false;
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

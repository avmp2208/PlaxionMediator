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
/// PlaxionMediator049: flags the same IRequestAuthorization check type registered more than once
/// for the same request type via AddPlaxionMediatorAuthorization&lt;TRequest, TCheck&gt;().
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DuplicateAuthorizationRegistrationAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(DiagnosticDescriptors.DuplicateAuthorizationRegistration);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            ConcurrentBag<(string RequestKey, string CheckKey, Location Location)> registrations = new();

            start.RegisterSyntaxNodeAction(ctx =>
            {
                if (ctx.Node is not InvocationExpressionSyntax invocation)
                {
                    return;
                }

                string? methodName = GetMethodName(invocation);
                if (methodName != "AddPlaxionMediatorAuthorization")
                {
                    return;
                }

                if (invocation.Expression is not MemberAccessExpressionSyntax
                    {
                        Name: GenericNameSyntax { TypeArgumentList.Arguments.Count: 2 } generic
                    })
                {
                    return;
                }

                TypeSyntax requestSyntax = generic.TypeArgumentList.Arguments[0];
                TypeSyntax checkSyntax = generic.TypeArgumentList.Arguments[1];

                ITypeSymbol? requestType = ctx.SemanticModel.GetTypeInfo(requestSyntax, ctx.CancellationToken).Type
                    ?? ctx.SemanticModel.GetSymbolInfo(requestSyntax, ctx.CancellationToken).Symbol as ITypeSymbol;
                ITypeSymbol? checkType = ctx.SemanticModel.GetTypeInfo(checkSyntax, ctx.CancellationToken).Type
                    ?? ctx.SemanticModel.GetSymbolInfo(checkSyntax, ctx.CancellationToken).Symbol as ITypeSymbol;

                if (requestType is null || checkType is null)
                {
                    return;
                }

                string requestKey = requestType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                string checkKey = checkType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                registrations.Add((requestKey, checkKey, generic.GetLocation()));
            }, SyntaxKind.InvocationExpression);

            start.RegisterCompilationEndAction(end =>
            {
                foreach (IGrouping<(string RequestKey, string CheckKey), (string RequestKey, string CheckKey, Location Location)> group in
                         registrations.GroupBy(r => (r.RequestKey, r.CheckKey)))
                {
                    List<(string RequestKey, string CheckKey, Location Location)> items = group.ToList();
                    if (items.Count < 2)
                    {
                        continue;
                    }

                    // Report on the second (and later) registration sites.
                    for (int i = 1; i < items.Count; i++)
                    {
                        string checkDisplay = SimplifyTypeName(items[i].CheckKey);
                        string requestDisplay = SimplifyTypeName(items[i].RequestKey);
                        end.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.DuplicateAuthorizationRegistration,
                            items[i].Location,
                            checkDisplay,
                            requestDisplay));
                    }
                }
            });
        });
    }

    private static string SimplifyTypeName(string fullyQualified)
    {
        // "global::Namespace.Type" -> "Type" (or last segment)
        string name = fullyQualified;
        if (name.StartsWith("global::", System.StringComparison.Ordinal))
        {
            name = name.Substring("global::".Length);
        }

        int lastDot = name.LastIndexOf('.');
        return lastDot >= 0 ? name.Substring(lastDot + 1) : name;
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

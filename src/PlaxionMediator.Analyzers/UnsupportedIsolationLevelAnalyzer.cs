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
/// PlaxionMediator045: best-effort warning when a request statically requests Snapshot isolation
/// while the registered EF provider is SQLite or InMemory (known unsupported).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UnsupportedIsolationLevelAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(DiagnosticDescriptors.UnsupportedIsolationLevel);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            INamedTypeSymbol? transactional = start.Compilation.GetTypeByMetadataName(AnalyzerHelpers.TransactionalRequestMetadataName);
            INamedTypeSymbol? isolationEnum = start.Compilation.GetTypeByMetadataName(AnalyzerHelpers.TransactionIsolationLevelMetadataName);
            if (transactional is null || isolationEnum is null)
            {
                return;
            }

            ConcurrentBag<byte> unsupportedProvider = new();
            ConcurrentBag<(INamedTypeSymbol Request, string Level, Location Location)> snapshotRequests = new();

            start.RegisterSyntaxNodeAction(ctx =>
            {
                if (ctx.Node is not InvocationExpressionSyntax invocation)
                {
                    return;
                }

                string? methodName = GetMethodName(invocation);
                if (methodName is "UseSqlite" or "UseInMemoryDatabase")
                {
                    unsupportedProvider.Add(0);
                }
            }, SyntaxKind.InvocationExpression);

            start.RegisterSymbolAction(ctx =>
            {
                if (ctx.Symbol is not INamedTypeSymbol type
                    || !AnalyzerHelpers.IsConcreteType(type)
                    || !type.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, transactional)))
                {
                    return;
                }

                // Look for IsolationLevel property returning Snapshot
                foreach (IPropertySymbol property in type.GetMembers().OfType<IPropertySymbol>())
                {
                    if (property.Name != "IsolationLevel")
                    {
                        continue;
                    }

                    foreach (SyntaxReference syntaxRef in property.DeclaringSyntaxReferences)
                    {
                        SyntaxNode syntax = syntaxRef.GetSyntax(ctx.CancellationToken);
                        string text = syntax.ToString();
                        if (text.Contains("Snapshot"))
                        {
                            Location location = syntax.GetLocation();
                            snapshotRequests.Add((type, "Snapshot", location));
                        }
                    }
                }
            }, SymbolKind.NamedType);

            start.RegisterCompilationEndAction(end =>
            {
                if (unsupportedProvider.IsEmpty)
                {
                    return;
                }

                foreach ((INamedTypeSymbol request, string level, Location location) in snapshotRequests)
                {
                    end.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.UnsupportedIsolationLevel,
                        location,
                        request.Name,
                        level));
                }
            });
        });
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

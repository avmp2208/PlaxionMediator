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
/// PlaxionMediator080: flags [HighFrequency] requests with too many open-generic pipeline behaviors registered.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UnnecessaryBehaviorOnHotPathAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(DiagnosticDescriptors.UnnecessaryBehaviorOnHotPath);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            INamedTypeSymbol? behaviorUnbound = start.Compilation.GetTypeByMetadataName(AnalyzerHelpers.PipelineBehaviorMetadataName);
            if (behaviorUnbound is null)
            {
                return;
            }

            ConcurrentBag<byte> behaviorRegistrations = new();

            start.RegisterSyntaxNodeAction(ctx =>
            {
                if (ctx.Node is not InvocationExpressionSyntax invocation
                    || invocation.ArgumentList.Arguments.Count < 2)
                {
                    return;
                }

                // services.AddX(typeof(IPipelineBehavior<,>), typeof(SomeBehavior<,>))
                if (invocation.ArgumentList.Arguments[0].Expression is not TypeOfExpressionSyntax serviceTypeOf)
                {
                    return;
                }

                ITypeSymbol? serviceType = ctx.SemanticModel.GetTypeInfo(serviceTypeOf.Type, ctx.CancellationToken).Type
                    ?? ctx.SemanticModel.GetSymbolInfo(serviceTypeOf.Type, ctx.CancellationToken).Symbol as ITypeSymbol;
                if (serviceType is not INamedTypeSymbol named)
                {
                    return;
                }

                INamedTypeSymbol definition = named.IsGenericType ? named.OriginalDefinition : named;
                if (!SymbolEqualityComparer.Default.Equals(definition, behaviorUnbound))
                {
                    return;
                }

                behaviorRegistrations.Add(0);
            }, SyntaxKind.InvocationExpression);

            start.RegisterCompilationEndAction(end =>
            {
                int count = behaviorRegistrations.Count;
                if (count <= AnalyzerHelpers.HotPathBehaviorThreshold)
                {
                    return;
                }

                INamedTypeSymbol? highFrequency = end.Compilation.GetTypeByMetadataName(AnalyzerHelpers.HighFrequencyAttributeMetadataName);
                INamedTypeSymbol? requestUnbound = end.Compilation.GetTypeByMetadataName(AnalyzerHelpers.RequestMetadataName);
                if (highFrequency is null || requestUnbound is null)
                {
                    return;
                }

                foreach (INamedTypeSymbol type in GetAllTypes(end.Compilation.Assembly.GlobalNamespace))
                {
                    if (!AnalyzerHelpers.IsConcreteType(type)
                        || !AnalyzerHelpers.ImplementsRequest(type, requestUnbound, out _))
                    {
                        continue;
                    }

                    bool hasAttr = type.GetAttributes().Any(a =>
                        SymbolEqualityComparer.Default.Equals(a.AttributeClass, highFrequency));
                    if (!hasAttr)
                    {
                        continue;
                    }

                    Location location = type.Locations.FirstOrDefault() ?? Location.None;
                    end.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.UnnecessaryBehaviorOnHotPath,
                        location,
                        type.Name,
                        count));
                }
            });
        });
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

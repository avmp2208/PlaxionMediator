using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace PlaxionMediator.Analyzers;

/// <summary>
/// PlaxionMediator002: flags request types that have more than one IRequestHandler implementation.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MultipleHandlersAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(DiagnosticDescriptors.MultipleHandlers);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(compilationContext =>
        {
            INamedTypeSymbol? handlerUnbound = compilationContext.Compilation.GetTypeByMetadataName(AnalyzerHelpers.RequestHandlerMetadataName);
            if (handlerUnbound is null)
            {
                return;
            }

            ConcurrentDictionary<INamedTypeSymbol, ConcurrentBag<INamedTypeSymbol>> handlerCounts =
                new ConcurrentDictionary<INamedTypeSymbol, ConcurrentBag<INamedTypeSymbol>>(SymbolEqualityComparer.Default);

            compilationContext.RegisterSymbolAction(symbolContext =>
            {
                if (symbolContext.Symbol is not INamedTypeSymbol type || !AnalyzerHelpers.IsConcreteType(type))
                {
                    return;
                }

                INamedTypeSymbol? handlerIface = AnalyzerHelpers.FindRequestHandlerInterface(type, handlerUnbound);
                if (handlerIface is null || handlerIface.TypeArguments[0] is not INamedTypeSymbol requestType)
                {
                    return;
                }

                ConcurrentBag<INamedTypeSymbol> bag = handlerCounts.GetOrAdd(
                    requestType,
                    _ => new ConcurrentBag<INamedTypeSymbol>());
                bag.Add(type);
            }, SymbolKind.NamedType);

            compilationContext.RegisterCompilationEndAction(endContext =>
            {
                foreach (KeyValuePair<INamedTypeSymbol, ConcurrentBag<INamedTypeSymbol>> pair in handlerCounts)
                {
                    List<INamedTypeSymbol> handlers = pair.Value.ToList();
                    if (handlers.Count <= 1)
                    {
                        continue;
                    }

                    foreach (INamedTypeSymbol handler in handlers)
                    {
                        Location location = handler.Locations.FirstOrDefault() ?? Location.None;
                        endContext.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.MultipleHandlers,
                            location,
                            pair.Key.Name));
                    }
                }
            });
        });
    }
}

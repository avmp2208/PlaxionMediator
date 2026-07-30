using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace PlaxionMediator.Analyzers;

/// <summary>
/// PlaxionMediator001: flags IRequest&lt;T&gt; types without a matching IRequestHandler in the compilation.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MissingHandlerAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(DiagnosticDescriptors.MissingHandler);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(compilationContext =>
        {
            INamedTypeSymbol? requestUnbound = compilationContext.Compilation.GetTypeByMetadataName(AnalyzerHelpers.RequestMetadataName);
            INamedTypeSymbol? handlerUnbound = compilationContext.Compilation.GetTypeByMetadataName(AnalyzerHelpers.RequestHandlerMetadataName);
            if (requestUnbound is null || handlerUnbound is null)
            {
                return;
            }

            ConcurrentDictionary<INamedTypeSymbol, byte> requests = new ConcurrentDictionary<INamedTypeSymbol, byte>(SymbolEqualityComparer.Default);
            ConcurrentDictionary<INamedTypeSymbol, byte> handledRequests = new ConcurrentDictionary<INamedTypeSymbol, byte>(SymbolEqualityComparer.Default);

            compilationContext.RegisterSymbolAction(symbolContext =>
            {
                if (symbolContext.Symbol is not INamedTypeSymbol type || !AnalyzerHelpers.IsConcreteType(type))
                {
                    return;
                }

                if (AnalyzerHelpers.ImplementsRequest(type, requestUnbound, out _))
                {
                    requests[type] = 0;
                }

                INamedTypeSymbol? handlerIface = AnalyzerHelpers.FindRequestHandlerInterface(type, handlerUnbound);
                if (handlerIface is not null && handlerIface.TypeArguments[0] is INamedTypeSymbol handled)
                {
                    handledRequests[handled] = 0;
                }
            }, SymbolKind.NamedType);

            compilationContext.RegisterCompilationEndAction(endContext =>
            {
                foreach (INamedTypeSymbol request in requests.Keys)
                {
                    if (handledRequests.ContainsKey(request))
                    {
                        continue;
                    }

                    Location location = request.Locations.FirstOrDefault() ?? Location.None;
                    endContext.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.MissingHandler,
                        location,
                        request.Name));
                }
            });
        });
    }
}

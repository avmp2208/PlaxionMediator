using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace PlaxionMediator.Analyzers;

/// <summary>
/// PlaxionMediator050: flags LINQ usage in handlers for [HighFrequency] requests.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class LinqUsageInHighFrequencyHandlerAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(DiagnosticDescriptors.LinqUsageInHighFrequencyHandler);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context)
    {
        if (context.Operation is not IInvocationOperation invocation)
        {
            return;
        }

        IMethodSymbol method = invocation.TargetMethod;
        // Check if it's a LINQ extension method
        if (!method.IsExtensionMethod || method.ContainingNamespace?.ToDisplayString() != "System.Linq")
        {
            return;
        }

        IMethodSymbol? containingMethod = context.ContainingSymbol as IMethodSymbol;
        if (containingMethod is null || !AnalyzerHelpers.IsHandleMethod(containingMethod))
        {
            return;
        }

        INamedTypeSymbol? handlerType = containingMethod.ContainingType;
        if (handlerType is null || !AnalyzerHelpers.IsHandlerType(handlerType, context.Compilation))
        {
            return;
        }

        ITypeSymbol? requestType = GetRequestType(handlerType, context.Compilation);
        if (requestType is null)
        {
            return;
        }

        INamedTypeSymbol? highFrequencyAttr = context.Compilation.GetTypeByMetadataName(AnalyzerHelpers.HighFrequencyAttributeMetadataName);
        if (highFrequencyAttr is null)
        {
            return;
        }

        bool isHighFrequency = requestType.GetAttributes().Any(a =>
            SymbolEqualityComparer.Default.Equals(a.AttributeClass, highFrequencyAttr));

        if (isHighFrequency)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.LinqUsageInHighFrequencyHandler,
                invocation.Syntax.GetLocation(),
                handlerType.Name,
                requestType.Name));
        }
    }

    private static ITypeSymbol? GetRequestType(INamedTypeSymbol handlerType, Compilation compilation)
    {
        INamedTypeSymbol? requestHandler = compilation.GetTypeByMetadataName(AnalyzerHelpers.RequestHandlerMetadataName);
        INamedTypeSymbol? notificationHandler = compilation.GetTypeByMetadataName(AnalyzerHelpers.NotificationHandlerMetadataName);
        INamedTypeSymbol? streamHandler = compilation.GetTypeByMetadataName(AnalyzerHelpers.StreamRequestHandlerMetadataName);

        foreach (var iface in handlerType.AllInterfaces)
        {
            if (!iface.IsGenericType) continue;

            if (SymbolEqualityComparer.Default.Equals(iface.OriginalDefinition, requestHandler) ||
                SymbolEqualityComparer.Default.Equals(iface.OriginalDefinition, streamHandler))
            {
                if (iface.TypeArguments.Length >= 1)
                    return iface.TypeArguments[0];
            }
            
            if (SymbolEqualityComparer.Default.Equals(iface.OriginalDefinition, notificationHandler))
            {
                if (iface.TypeArguments.Length == 1)
                    return iface.TypeArguments[0];
            }
        }

        return null;
    }
}

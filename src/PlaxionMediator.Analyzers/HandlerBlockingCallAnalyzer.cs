using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace PlaxionMediator.Analyzers;

/// <summary>
/// PlaxionMediator006: flags sync-over-async blocking calls inside handler Handle methods.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class HandlerBlockingCallAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(DiagnosticDescriptors.HandlerBlockingCall);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(AnalyzePropertyReference, OperationKind.PropertyReference);
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
    }

    private static void AnalyzePropertyReference(OperationAnalysisContext context)
    {
        if (context.Operation is not IPropertyReferenceOperation propertyReference)
        {
            return;
        }

        if (propertyReference.Property.Name != "Result")
        {
            return;
        }

        INamedTypeSymbol? containingType = propertyReference.Property.ContainingType;
        if (containingType is null || !IsTaskLikeType(containingType))
        {
            return;
        }

        ReportIfInsideHandler(context, propertyReference.Syntax, ".Result");
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context)
    {
        if (context.Operation is not IInvocationOperation invocation)
        {
            return;
        }

        IMethodSymbol method = invocation.TargetMethod;
        string display;

        if (method.Name == "Wait"
            && method.Parameters.Length <= 1
            && method.ContainingType is not null
            && IsTaskLikeType(method.ContainingType))
        {
            display = ".Wait()";
        }
        else if (method.Name == "GetResult"
                 && method.ContainingType is { Name: "TaskAwaiter" or "ValueTaskAwaiter" or "ConfiguredTaskAwaiter" or "ConfiguredValueTaskAwaiter" }
                 && IsGetAwaiterGetResultChain(invocation.Syntax))
        {
            display = ".GetAwaiter().GetResult()";
        }
        else
        {
            return;
        }

        ReportIfInsideHandler(context, invocation.Syntax, display);
    }

    private static bool IsGetAwaiterGetResultChain(SyntaxNode syntax)
    {
        // Prefer flagging the common anti-pattern expression: something.GetAwaiter().GetResult()
        if (syntax is not InvocationExpressionSyntax getResultInvocation)
        {
            return false;
        }

        if (getResultInvocation.Expression is not MemberAccessExpressionSyntax getResultAccess
            || getResultAccess.Name.Identifier.Text != "GetResult")
        {
            return false;
        }

        return getResultAccess.Expression is InvocationExpressionSyntax getAwaiterInvocation
               && getAwaiterInvocation.Expression is MemberAccessExpressionSyntax getAwaiterAccess
               && getAwaiterAccess.Name.Identifier.Text == "GetAwaiter";
    }

    private static void ReportIfInsideHandler(OperationAnalysisContext context, SyntaxNode syntax, string callDisplay)
    {
        IMethodSymbol? containingMethod = GetContainingMethod(context.Operation);
        if (containingMethod is null || containingMethod.Name != "Handle")
        {
            return;
        }

        if (!IsHandlerImplementation(containingMethod, context.Compilation))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.HandlerBlockingCall,
            syntax.GetLocation(),
            callDisplay,
            containingMethod.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
    }

    private static IMethodSymbol? GetContainingMethod(IOperation operation)
    {
        ISymbol? symbol = operation.SemanticModel?.GetEnclosingSymbol(operation.Syntax.SpanStart);
        while (symbol is not null)
        {
            if (symbol is IMethodSymbol method)
            {
                return method;
            }

            symbol = symbol.ContainingSymbol;
        }

        return null;
    }

    private static bool IsHandlerImplementation(IMethodSymbol method, Compilation compilation)
    {
        INamedTypeSymbol? containingType = method.ContainingType;
        if (containingType is null || !AnalyzerHelpers.IsConcreteType(containingType))
        {
            return false;
        }

        INamedTypeSymbol? requestHandler = compilation.GetTypeByMetadataName(AnalyzerHelpers.RequestHandlerMetadataName);
        INamedTypeSymbol? notificationHandler = compilation.GetTypeByMetadataName(AnalyzerHelpers.NotificationHandlerMetadataName);

        bool isRequestHandler = requestHandler is not null
            && containingType.AllInterfaces.Any(i =>
                i.IsGenericType && SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, requestHandler));

        bool isNotificationHandler = notificationHandler is not null
            && containingType.AllInterfaces.Any(i =>
                i.IsGenericType && SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, notificationHandler));

        if (!isRequestHandler && !isNotificationHandler)
        {
            return false;
        }

        // Include both public Handle and explicit interface implementations named Handle.
        if (method.MethodKind is MethodKind.ExplicitInterfaceImplementation)
        {
            return method.Name is "Handle"
                   || method.ExplicitInterfaceImplementations.Any(impl => impl.Name == "Handle");
        }

        return method is { Name: "Handle", MethodKind: MethodKind.Ordinary };
    }

    private static bool IsTaskLikeType(INamedTypeSymbol type)
    {
        INamedTypeSymbol original = type.OriginalDefinition;
        string name = original.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return name is "global::System.Threading.Tasks.Task"
            or "global::System.Threading.Tasks.Task<TResult>"
            or "global::System.Threading.Tasks.ValueTask"
            or "global::System.Threading.Tasks.ValueTask<TResult>";
    }
}

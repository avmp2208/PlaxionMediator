using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace PlaxionMediator.Analyzers;

/// <summary>
/// PlaxionMediator031: flags awaited calls that could accept CancellationToken but are not passed the ambient token.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MissingCancellationTokenPropagationAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => ImmutableArray.Create(DiagnosticDescriptors.MissingCancellationTokenPropagation);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(AnalyzeAwait, OperationKind.Await);
    }

    private static void AnalyzeAwait(OperationAnalysisContext context)
    {
        if (context.Operation is not IAwaitOperation awaitOperation)
        {
            return;
        }

        IOperation awaited = awaitOperation.Operation;

        // Unwrap ConfigureAwait: await foo().ConfigureAwait(false)
        if (awaited is IInvocationOperation maybeConfigure
            && maybeConfigure.TargetMethod.Name == "ConfigureAwait")
        {
            if (maybeConfigure.Instance is IInvocationOperation targetInvocation)
            {
                awaited = targetInvocation;
            }
            else
            {
                return;
            }
        }

        if (awaited is not IInvocationOperation invocation)
        {
            return;
        }

        IMethodSymbol method = invocation.TargetMethod;
        IMethodSymbol? containing = GetContainingMethod(context.Operation);
        if (containing is null || !AnalyzerHelpers.IsHandleMethod(containing))
        {
            return;
        }

        if (containing.ContainingType is null
            || !AnalyzerHelpers.IsHandlerOrBehaviorType(containing.ContainingType, context.Compilation))
        {
            return;
        }

        INamedTypeSymbol? ctType = context.Compilation.GetTypeByMetadataName("System.Threading.CancellationToken");
        if (ctType is null)
        {
            return;
        }

        IParameterSymbol? ambientToken = containing.Parameters
            .FirstOrDefault(p => SymbolEqualityComparer.Default.Equals(p.Type, ctType));
        if (ambientToken is null)
        {
            return;
        }

        // Already passing a CancellationToken explicitly.
        if (invocation.Arguments.Any(a =>
                a.ArgumentKind == ArgumentKind.Explicit
                && a.Parameter is not null
                && SymbolEqualityComparer.Default.Equals(a.Parameter.Type, ctType)))
        {
            return;
        }

        // Case 1: invoked method has a CancellationToken parameter that was defaulted.
        bool hasDefaultedToken = method.Parameters.Any(p =>
            SymbolEqualityComparer.Default.Equals(p.Type, ctType));

        // Case 2: an overload exists that accepts one more CancellationToken argument.
        bool hasTokenOverload = !hasDefaultedToken && HasCancellationTokenOverload(method, ctType, invocation.Arguments.Length);

        if (!hasDefaultedToken && !hasTokenOverload)
        {
            return;
        }

        string callName = method.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.MissingCancellationTokenPropagation,
            invocation.Syntax.GetLocation(),
            ambientToken.Name,
            callName));
    }

    private static bool HasCancellationTokenOverload(IMethodSymbol method, INamedTypeSymbol ctType, int suppliedArgCount)
    {
        INamedTypeSymbol? containingType = method.ContainingType;
        if (containingType is null)
        {
            return false;
        }

        foreach (IMethodSymbol candidate in containingType.GetMembers(method.Name).OfType<IMethodSymbol>())
        {
            if (candidate.IsStatic != method.IsStatic)
            {
                continue;
            }

            // Looking for overload: same leading parameters + trailing CancellationToken.
            if (candidate.Parameters.Length != suppliedArgCount + 1)
            {
                continue;
            }

            IParameterSymbol last = candidate.Parameters[candidate.Parameters.Length - 1];
            if (!SymbolEqualityComparer.Default.Equals(last.Type, ctType))
            {
                continue;
            }

            bool leadingMatch = true;
            for (int i = 0; i < suppliedArgCount; i++)
            {
                if (!SymbolEqualityComparer.Default.Equals(candidate.Parameters[i].Type, method.Parameters[i].Type)
                    && !IsCompatible(candidate.Parameters[i].Type, method.Parameters[i].Type))
                {
                    leadingMatch = false;
                    break;
                }
            }

            if (leadingMatch)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsCompatible(ITypeSymbol left, ITypeSymbol right)
    {
        // int vs int32 etc. — SymbolEqualityComparer handles identity; allow conversion-compatible primitives via name.
        return left.SpecialType != SpecialType.None
               && left.SpecialType == right.SpecialType;
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
}

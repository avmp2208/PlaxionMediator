using PlaxionMediator.Analyzers;
using Xunit;

namespace PlaxionMediator.Analyzers.Tests;

public sealed class AuthorizationRegisteredBehindCacheAnalyzerTests
{
    [Fact]
    public async Task AuthorizationBehindCache_ReportsError()
    {
        // Caching registered before Authorization => Caching outer, Authorization inner (unsafe)
        const string source = """
            using PlaxionMediator;
            using PlaxionMediator.Authorization;
            using PlaxionMediator.Caching;
            public static class Startup
            {
                public static void Configure(PlaxionMediatorOptions o)
                {
                    o.UsePlaxionMediatorCachingBehavior();
                    o.UsePlaxionMediatorAuthorizationBehavior();
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new AuthorizationRegisteredBehindCacheAnalyzer(), source);
        Assert.Contains(diagnostics, d => d.Id == "PlaxionMediator046");
    }

    [Fact]
    public async Task AuthorizationOuterToCache_NoDiagnostic()
    {
        // Authorization registered before Caching => Authorization outer, Caching inner (safe / recommended)
        const string source = """
            using PlaxionMediator;
            using PlaxionMediator.Authorization;
            using PlaxionMediator.Caching;
            public static class Startup
            {
                public static void Configure(PlaxionMediatorOptions o)
                {
                    o.UsePlaxionMediatorAuthorizationBehavior();
                    o.UsePlaxionMediatorCachingBehavior();
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new AuthorizationRegisteredBehindCacheAnalyzer(), source);
        Assert.DoesNotContain(diagnostics, d => d.Id == "PlaxionMediator046");
    }
}

public sealed class UnregisteredAuthorizationBehaviorAnalyzerTests
{
    [Fact]
    public async Task AuthorizationCheck_MissingBehavior_ReportsError()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using PlaxionMediator.Abstractions;
            using PlaxionMediator.Authorization;
            public sealed record SecureRequest(string Id) : IRequest<string>;
            public sealed class SecureRequestAuth : IRequestAuthorization<SecureRequest>
            {
                public ValueTask<AuthorizationOutcome> AuthorizeAsync(
                    SecureRequest request,
                    IAuthorizationContext context,
                    CancellationToken cancellationToken)
                    => ValueTask.FromResult(AuthorizationOutcome.Authorized);
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new UnregisteredAuthorizationBehaviorAnalyzer(), source);
        Assert.Contains(diagnostics, d => d.Id == "PlaxionMediator047");
    }

    [Fact]
    public async Task AuthorizationCheck_WithBehavior_NoDiagnostic()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using PlaxionMediator;
            using PlaxionMediator.Abstractions;
            using PlaxionMediator.Authorization;
            public sealed record SecureRequest(string Id) : IRequest<string>;
            public sealed class SecureRequestAuth : IRequestAuthorization<SecureRequest>
            {
                public ValueTask<AuthorizationOutcome> AuthorizeAsync(
                    SecureRequest request,
                    IAuthorizationContext context,
                    CancellationToken cancellationToken)
                    => ValueTask.FromResult(AuthorizationOutcome.Authorized);
            }
            public static class Startup
            {
                public static void Configure(PlaxionMediatorOptions o)
                {
                    o.UsePlaxionMediatorAuthorizationBehavior();
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new UnregisteredAuthorizationBehaviorAnalyzer(), source);
        Assert.DoesNotContain(diagnostics, d => d.Id == "PlaxionMediator047");
    }
}

public sealed class AmbiguousAuthorizationOrderingAnalyzerTests
{
    [Fact]
    public async Task AuthorizationInsideRetry_ReportsWarning()
    {
        // Retry registered before Authorization => Retry outer, Authorization inner (unsafe)
        const string source = """
            using PlaxionMediator;
            using PlaxionMediator.Authorization;
            using PlaxionMediator.Retry;
            public static class Startup
            {
                public static void Configure(PlaxionMediatorOptions o)
                {
                    o.UsePlaxionMediatorRetryBehavior();
                    o.UsePlaxionMediatorAuthorizationBehavior();
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new AmbiguousAuthorizationOrderingAnalyzer(), source);
        Assert.Contains(diagnostics, d => d.Id == "PlaxionMediator048");
    }

    [Fact]
    public async Task AuthorizationOuterToRetryAndTransaction_NoDiagnostic()
    {
        // Authorization registered before Retry/Transaction => Authorization outer (safe / recommended)
        const string source = """
            using PlaxionMediator;
            using PlaxionMediator.Authorization;
            using PlaxionMediator.Retry;
            using PlaxionMediator.Transactions;
            public static class Startup
            {
                public static void Configure(PlaxionMediatorOptions o)
                {
                    o.UsePlaxionMediatorAuthorizationBehavior();
                    o.UsePlaxionMediatorRetryBehavior();
                    o.UsePlaxionMediatorTransactionBehavior();
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new AmbiguousAuthorizationOrderingAnalyzer(), source);
        Assert.DoesNotContain(diagnostics, d => d.Id == "PlaxionMediator048");
    }
}

public sealed class DuplicateAuthorizationRegistrationAnalyzerTests
{
    [Fact]
    public async Task DuplicateSameCheck_ReportsWarning()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using Microsoft.Extensions.DependencyInjection;
            using PlaxionMediator.Abstractions;
            using PlaxionMediator.Authorization;
            public sealed record SecureRequest(string Id) : IRequest<string>;
            public sealed class SecureRequestAuth : IRequestAuthorization<SecureRequest>
            {
                public ValueTask<AuthorizationOutcome> AuthorizeAsync(
                    SecureRequest request,
                    IAuthorizationContext context,
                    CancellationToken cancellationToken)
                    => ValueTask.FromResult(AuthorizationOutcome.Authorized);
            }
            public static class Startup
            {
                public static void Configure(IServiceCollection services)
                {
                    services.AddPlaxionMediatorAuthorization<SecureRequest, SecureRequestAuth>();
                    services.AddPlaxionMediatorAuthorization<SecureRequest, SecureRequestAuth>();
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new DuplicateAuthorizationRegistrationAnalyzer(), source);
        Assert.Contains(diagnostics, d => d.Id == "PlaxionMediator049");
    }

    [Fact]
    public async Task DifferentChecksSameRequest_NoDiagnostic()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using Microsoft.Extensions.DependencyInjection;
            using PlaxionMediator.Abstractions;
            using PlaxionMediator.Authorization;
            public sealed record SecureRequest(string Id) : IRequest<string>;
            public sealed class SecureRequestAuthA : IRequestAuthorization<SecureRequest>
            {
                public ValueTask<AuthorizationOutcome> AuthorizeAsync(
                    SecureRequest request,
                    IAuthorizationContext context,
                    CancellationToken cancellationToken)
                    => ValueTask.FromResult(AuthorizationOutcome.Authorized);
            }
            public sealed class SecureRequestAuthB : IRequestAuthorization<SecureRequest>
            {
                public ValueTask<AuthorizationOutcome> AuthorizeAsync(
                    SecureRequest request,
                    IAuthorizationContext context,
                    CancellationToken cancellationToken)
                    => ValueTask.FromResult(AuthorizationOutcome.Authorized);
            }
            public static class Startup
            {
                public static void Configure(IServiceCollection services)
                {
                    services.AddPlaxionMediatorAuthorization<SecureRequest, SecureRequestAuthA>();
                    services.AddPlaxionMediatorAuthorization<SecureRequest, SecureRequestAuthB>();
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new DuplicateAuthorizationRegistrationAnalyzer(), source);
        Assert.DoesNotContain(diagnostics, d => d.Id == "PlaxionMediator049");
    }
}

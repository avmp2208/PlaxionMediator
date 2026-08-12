using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace PlaxionMediator.SourceGenerators.Tests;

public sealed class AuthorizationGeneratorTests
{
    private const string HandlerOnlySource = """
        using System.Threading;
        using System.Threading.Tasks;
        using PlaxionMediator.Abstractions;

        namespace Demo;

        public sealed record Ping(string Message) : IRequest<string>;

        public sealed class PingHandler : IRequestHandler<Ping, string>
        {
            public ValueTask<string> Handle(Ping request, CancellationToken cancellationToken)
                => ValueTask.FromResult("Pong:" + request.Message);
        }
        """;

    private const string SingleCheckSource = """
        using System.Threading;
        using System.Threading.Tasks;
        using PlaxionMediator.Abstractions;
        using PlaxionMediator.Authorization;

        namespace Demo;

        public sealed record Ping(string Message) : IRequest<string>;

        public sealed class PingHandler : IRequestHandler<Ping, string>
        {
            public ValueTask<string> Handle(Ping request, CancellationToken cancellationToken)
                => ValueTask.FromResult("Pong:" + request.Message);
        }

        public sealed class PingAuthorization : IRequestAuthorization<Ping>
        {
            public ValueTask<AuthorizationOutcome> AuthorizeAsync(
                Ping request,
                IAuthorizationContext context,
                CancellationToken cancellationToken)
                => ValueTask.FromResult(AuthorizationOutcome.Authorized);
        }
        """;

    private const string MultipleChecksSource = """
        using System.Threading;
        using System.Threading.Tasks;
        using PlaxionMediator.Abstractions;
        using PlaxionMediator.Authorization;

        namespace Demo;

        public sealed record Ping(string Message) : IRequest<string>;

        public sealed class PingHandler : IRequestHandler<Ping, string>
        {
            public ValueTask<string> Handle(Ping request, CancellationToken cancellationToken)
                => ValueTask.FromResult("Pong:" + request.Message);
        }

        // Intentionally reverse alphabetical declaration order vs expected emission order.
        public sealed class ZuluAuthorization : IRequestAuthorization<Ping>
        {
            public ValueTask<AuthorizationOutcome> AuthorizeAsync(
                Ping request,
                IAuthorizationContext context,
                CancellationToken cancellationToken)
                => ValueTask.FromResult(AuthorizationOutcome.Authorized);
        }

        public sealed class AlphaAuthorization : IRequestAuthorization<Ping>
        {
            public ValueTask<AuthorizationOutcome> AuthorizeAsync(
                Ping request,
                IAuthorizationContext context,
                CancellationToken cancellationToken)
                => ValueTask.FromResult(AuthorizationOutcome.Authorized);
        }

        public sealed class MikeAuthorization : IRequestAuthorization<Ping>
        {
            public ValueTask<AuthorizationOutcome> AuthorizeAsync(
                Ping request,
                IAuthorizationContext context,
                CancellationToken cancellationToken)
                => ValueTask.FromResult(AuthorizationOutcome.Authorized);
        }
        """;

    [Fact]
    public void Emits_No_Authorization_Registration_When_No_Checks_Exist()
    {
        // Authorization assembly is referenced, but no IRequestAuthorization<> implementations exist.
        (_, ImmutableArray<Diagnostic> diagnostics, GeneratorDriverRunResult runResult) =
            GeneratorTestHelper.Run(HandlerOnlySource, includeAuthorization: true);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);

        string registration = runResult.GeneratedTrees
            .First(t => t.FilePath.Contains("PlaxionMediatorRegistration", StringComparison.Ordinal))
            .GetText()
            .ToString();

        Assert.DoesNotContain("IRequestAuthorization<", registration);
        Assert.DoesNotContain("using PlaxionMediator.Authorization;", registration);
        Assert.Contains("PingHandler", registration);
    }

    [Fact]
    public void Emits_No_Authorization_Registration_When_Authorization_Assembly_Not_Referenced()
    {
        (_, ImmutableArray<Diagnostic> diagnostics, GeneratorDriverRunResult runResult) =
            GeneratorTestHelper.Run(HandlerOnlySource, includeAuthorization: false);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);

        string registration = runResult.GeneratedTrees
            .First(t => t.FilePath.Contains("PlaxionMediatorRegistration", StringComparison.Ordinal))
            .GetText()
            .ToString();

        Assert.DoesNotContain("IRequestAuthorization<", registration);
        Assert.DoesNotContain("using PlaxionMediator.Authorization;", registration);
    }

    [Fact]
    public void Registers_Single_Authorization_Check_For_Request_Type()
    {
        (Compilation compilation, ImmutableArray<Diagnostic> diagnostics, GeneratorDriverRunResult runResult) =
            GeneratorTestHelper.Run(SingleCheckSource, includeAuthorization: true);

        Diagnostic[] errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);

        string registration = runResult.GeneratedTrees
            .First(t => t.FilePath.Contains("PlaxionMediatorRegistration", StringComparison.Ordinal))
            .GetText()
            .ToString();

        Assert.Contains("using PlaxionMediator.Authorization;", registration);
        Assert.Contains(
            "services.TryAddEnumerable(new ServiceDescriptor(typeof(IRequestAuthorization<global::Demo.Ping>), typeof(global::Demo.PingAuthorization), ServiceLifetime.Scoped));",
            registration);

        Diagnostic[] compileErrors = compilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToArray();
        Assert.True(
            compileErrors.Length == 0,
            string.Join(Environment.NewLine, compileErrors.Select(e => e.ToString())));
    }

    [Fact]
    public void Registers_Multiple_Checks_For_Same_Request_In_Stable_Order()
    {
        (_, ImmutableArray<Diagnostic> diagnostics, GeneratorDriverRunResult runResult) =
            GeneratorTestHelper.Run(MultipleChecksSource, includeAuthorization: true);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);

        string registration = runResult.GeneratedTrees
            .First(t => t.FilePath.Contains("PlaxionMediatorRegistration", StringComparison.Ordinal))
            .GetText()
            .ToString();

        int alpha = registration.IndexOf("global::Demo.AlphaAuthorization", StringComparison.Ordinal);
        int mike = registration.IndexOf("global::Demo.MikeAuthorization", StringComparison.Ordinal);
        int zulu = registration.IndexOf("global::Demo.ZuluAuthorization", StringComparison.Ordinal);

        Assert.True(alpha >= 0, "AlphaAuthorization missing from generated registration.");
        Assert.True(mike >= 0, "MikeAuthorization missing from generated registration.");
        Assert.True(zulu >= 0, "ZuluAuthorization missing from generated registration.");
        Assert.True(alpha < mike && mike < zulu, "Authorization checks must be ordered by check FQN ascending.");

        // Re-run to confirm deterministic ordering across incremental executions.
        (_, _, GeneratorDriverRunResult secondRun) =
            GeneratorTestHelper.Run(MultipleChecksSource, includeAuthorization: true);
        string secondRegistration = secondRun.GeneratedTrees
            .First(t => t.FilePath.Contains("PlaxionMediatorRegistration", StringComparison.Ordinal))
            .GetText()
            .ToString();

        Assert.Equal(registration, secondRegistration);
    }

    [Fact]
    public void Non_Semantic_Edit_Does_Not_Change_Authorization_Registration()
    {
        const string edited = """
            using System.Threading;
            using System.Threading.Tasks;
            using PlaxionMediator.Abstractions;
            using PlaxionMediator.Authorization;

            namespace Demo;

            // blank lines / comments shift source locations without semantic change


            public sealed record Ping(string Message) : IRequest<string>;

            public sealed class PingHandler : IRequestHandler<Ping, string>
            {
                public ValueTask<string> Handle(Ping request, CancellationToken cancellationToken)
                    => ValueTask.FromResult("Pong:" + request.Message);
            }

            public sealed class PingAuthorization : IRequestAuthorization<Ping>
            {
                public ValueTask<AuthorizationOutcome> AuthorizeAsync(
                    Ping request,
                    IAuthorizationContext context,
                    CancellationToken cancellationToken)
                    => ValueTask.FromResult(AuthorizationOutcome.Authorized);
            }
            """;

        (_, _, GeneratorDriverRunResult baseline) =
            GeneratorTestHelper.Run(SingleCheckSource, includeAuthorization: true);
        (_, _, GeneratorDriverRunResult afterEdit) =
            GeneratorTestHelper.Run(edited, includeAuthorization: true);

        string baselineRegistration = baseline.GeneratedTrees
            .First(t => t.FilePath.Contains("PlaxionMediatorRegistration", StringComparison.Ordinal))
            .GetText()
            .ToString();
        string editedRegistration = afterEdit.GeneratedTrees
            .First(t => t.FilePath.Contains("PlaxionMediatorRegistration", StringComparison.Ordinal))
            .GetText()
            .ToString();

        Assert.Equal(baselineRegistration, editedRegistration);
    }

    [Fact]
    public void Skips_Abstract_And_Open_Generic_Authorization_Checks()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using PlaxionMediator.Abstractions;
            using PlaxionMediator.Authorization;

            namespace Demo;

            public sealed record Ping(string Message) : IRequest<string>;

            public sealed class PingHandler : IRequestHandler<Ping, string>
            {
                public ValueTask<string> Handle(Ping request, CancellationToken cancellationToken)
                    => ValueTask.FromResult("ok");
            }

            public abstract class AbstractPingAuth : IRequestAuthorization<Ping>
            {
                public abstract ValueTask<AuthorizationOutcome> AuthorizeAsync(
                    Ping request,
                    IAuthorizationContext context,
                    CancellationToken cancellationToken);
            }

            public sealed class GenericPingAuth<T> : IRequestAuthorization<Ping>
            {
                public ValueTask<AuthorizationOutcome> AuthorizeAsync(
                    Ping request,
                    IAuthorizationContext context,
                    CancellationToken cancellationToken)
                    => ValueTask.FromResult(AuthorizationOutcome.Authorized);
            }

            public sealed class ConcretePingAuth : IRequestAuthorization<Ping>
            {
                public ValueTask<AuthorizationOutcome> AuthorizeAsync(
                    Ping request,
                    IAuthorizationContext context,
                    CancellationToken cancellationToken)
                    => ValueTask.FromResult(AuthorizationOutcome.Authorized);
            }
            """;

        (_, ImmutableArray<Diagnostic> diagnostics, GeneratorDriverRunResult runResult) =
            GeneratorTestHelper.Run(source, includeAuthorization: true);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);

        string registration = runResult.GeneratedTrees
            .First(t => t.FilePath.Contains("PlaxionMediatorRegistration", StringComparison.Ordinal))
            .GetText()
            .ToString();

        Assert.Contains("global::Demo.ConcretePingAuth", registration);
        Assert.DoesNotContain("AbstractPingAuth", registration);
        Assert.DoesNotContain("GenericPingAuth", registration);
    }
}

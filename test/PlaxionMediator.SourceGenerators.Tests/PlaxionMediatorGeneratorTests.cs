using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace PlaxionMediator.SourceGenerators.Tests;

public sealed class PlaxionMediatorGeneratorTests
{
    [Fact]
    public void Generates_Registration_And_Sender_For_Simple_Request_Handler()
    {
        const string source = """
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

        (Compilation compilation, ImmutableArray<Diagnostic> diagnostics, GeneratorDriverRunResult runResult) =
            GeneratorTestHelper.Run(source);

        Diagnostic[] errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);

        Assert.Contains(runResult.GeneratedTrees, t => t.FilePath.EndsWith("PlaxionMediatorRegistration.g.cs", StringComparison.OrdinalIgnoreCase)
            || t.FilePath.Contains("PlaxionMediatorRegistration", StringComparison.Ordinal));
        Assert.Contains(runResult.GeneratedTrees, t => t.FilePath.Contains("PlaxionMediatorSender", StringComparison.Ordinal));

        string registration = runResult.GeneratedTrees
            .First(t => t.FilePath.Contains("PlaxionMediatorRegistration", StringComparison.Ordinal))
            .GetText()
            .ToString();

        Assert.Contains("IRequestHandler<", registration);
        Assert.Contains("PingHandler", registration);
        Assert.Contains("PlaxionMediatorGeneratedRegistrationBridge.Register", registration);

        string sender = runResult.GeneratedTrees
            .First(t => t.FilePath.Contains("PlaxionMediatorSender", StringComparison.Ordinal))
            .GetText()
            .ToString();

        Assert.Contains("class PlaxionMediatorSender", sender);
        Assert.Contains("case global::Demo.Ping", sender);

        // Generated code should compile with the original compilation (may have other unrelated warnings)
        ImmutableArray<Diagnostic> compileDiagnostics = compilation.GetDiagnostics();
        Diagnostic[] compileErrors = compileDiagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.True(
            compileErrors.Length == 0,
            string.Join(Environment.NewLine, compileErrors.Select(e => e.ToString())));
    }

    [Fact]
    public void Reports_PlaxionMediator001_When_Handler_Is_Missing()
    {
        const string source = """
            using PlaxionMediator.Abstractions;

            namespace Demo;

            public sealed record OrphanRequest(string Message) : IRequest<string>;
            """;

        (_, ImmutableArray<Diagnostic> diagnostics, _) = GeneratorTestHelper.Run(source);

        Assert.Contains(diagnostics, d => d.Id == "PlaxionMediator001");
    }

    [Fact]
    public void Reports_PlaxionMediator002_When_Multiple_Handlers_Exist()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using PlaxionMediator.Abstractions;

            namespace Demo;

            public sealed record Ping(string Message) : IRequest<string>;

            public sealed class PingHandlerA : IRequestHandler<Ping, string>
            {
                public ValueTask<string> Handle(Ping request, CancellationToken cancellationToken)
                    => ValueTask.FromResult("A");
            }

            public sealed class PingHandlerB : IRequestHandler<Ping, string>
            {
                public ValueTask<string> Handle(Ping request, CancellationToken cancellationToken)
                    => ValueTask.FromResult("B");
            }
            """;

        (_, ImmutableArray<Diagnostic> diagnostics, _) = GeneratorTestHelper.Run(source);

        Assert.Contains(diagnostics, d => d.Id == "PlaxionMediator002");
    }

    [Fact]
    public void Generates_Notification_Handler_Registration()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using PlaxionMediator.Abstractions;

            namespace Demo;

            public sealed record SomethingHappened(string Id) : INotification;

            public sealed class SomethingHappenedHandler : INotificationHandler<SomethingHappened>
            {
                public ValueTask Handle(SomethingHappened notification, CancellationToken cancellationToken)
                    => default;
            }
            """;

        (_, ImmutableArray<Diagnostic> diagnostics, GeneratorDriverRunResult runResult) =
            GeneratorTestHelper.Run(source);

        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error && d.Id.StartsWith("PlaxionMediator", StringComparison.Ordinal));

        string registration = runResult.GeneratedTrees
            .First(t => t.FilePath.Contains("PlaxionMediatorRegistration", StringComparison.Ordinal))
            .GetText()
            .ToString();

        Assert.Contains("INotificationHandler<", registration);
        Assert.Contains("SomethingHappenedHandler", registration);
    }

    [Fact]
    public void Generates_Multiple_Notification_Handlers_Dispatch()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using PlaxionMediator.Abstractions;

            namespace Demo;

            public sealed record Event(string Id) : INotification;

            public sealed class HandlerA : INotificationHandler<Event>
            {
                public ValueTask Handle(Event notification, CancellationToken cancellationToken) => default;
            }

            public sealed class HandlerB : INotificationHandler<Event>
            {
                public ValueTask Handle(Event notification, CancellationToken cancellationToken) => default;
            }
            """;

        (_, _, GeneratorDriverRunResult runResult) = GeneratorTestHelper.Run(source);

        string sender = runResult.GeneratedTrees
            .First(t => t.FilePath.Contains("PlaxionMediatorSender", StringComparison.Ordinal))
            .GetText()
            .ToString();

        // Should call both handlers
        Assert.Contains("GetServices<INotificationHandler<global::Demo.Event>>", sender);
        Assert.Contains("foreach (INotificationHandler<global::Demo.Event> handler in handlers)", sender);
        Assert.Contains("await handler.Handle(notification, cancellationToken)", sender);
    }
}

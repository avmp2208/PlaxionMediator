using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Xunit;

namespace PlaxionMediator.SourceGenerators.Tests;

public sealed class GeneratorContractTests
{
    [Fact]
    public void Abstract_Handler_Is_Ignored()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using PlaxionMediator.Abstractions;

            namespace Demo;

            public sealed record Ping(string Message) : IRequest<string>;

            public abstract class BasePingHandler : IRequestHandler<Ping, string>
            {
                public abstract ValueTask<string> Handle(Ping request, CancellationToken cancellationToken);
            }
            """;

        (_, ImmutableArray<Diagnostic> diagnostics, GeneratorDriverRunResult runResult) =
            GeneratorTestHelper.Run(source);

        // Should report missing handler because the abstract one is ignored
        Assert.Contains(diagnostics, d => d.Id == "PlaxionMediator001");
        
        string registration = runResult.GeneratedTrees
            .First(t => t.FilePath.Contains("PlaxionMediatorRegistration", StringComparison.Ordinal))
            .GetText()
            .ToString();

        Assert.DoesNotContain("BasePingHandler", registration);
    }

    [Fact]
    public void Generic_Handler_Is_Ignored()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using PlaxionMediator.Abstractions;

            namespace Demo;

            public sealed record Ping(string Message) : IRequest<string>;

            public sealed class GenericHandler<T> : IRequestHandler<Ping, string>
            {
                public ValueTask<string> Handle(Ping request, CancellationToken cancellationToken)
                    => ValueTask.FromResult("Pong");
            }
            """;

        (_, ImmutableArray<Diagnostic> diagnostics, GeneratorDriverRunResult runResult) =
            GeneratorTestHelper.Run(source);

        // Currently ignored by IsConcreteNamedType
        Assert.Contains(diagnostics, d => d.Id == "PlaxionMediator001");

        string registration = runResult.GeneratedTrees
            .First(t => t.FilePath.Contains("PlaxionMediatorRegistration", StringComparison.Ordinal))
            .GetText()
            .ToString();

        Assert.DoesNotContain("GenericHandler", registration);
    }

    [Fact]
    public void Static_Handler_Is_Ignored()
    {
        // Note: static classes cannot implement interfaces in C#, but we test the generator's robustness.
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using PlaxionMediator.Abstractions;

            namespace Demo;

            public sealed record Ping(string Message) : IRequest<string>;

            public static class StaticHandler
            {
                // Cannot implement interface, but let's see if generator is confused by naming or something
            }
            """;

        (_, ImmutableArray<Diagnostic> diagnostics, _) = GeneratorTestHelper.Run(source);
        Assert.Contains(diagnostics, d => d.Id == "PlaxionMediator001");
    }

    [Fact]
    public void Open_Generic_Request_Is_Ignored()
    {
        const string source = """
            using PlaxionMediator.Abstractions;

            namespace Demo;

            public sealed record GenericRequest<T>(T Data) : IRequest<T>;
            """;

        (_, ImmutableArray<Diagnostic> diagnostics, _) = GeneratorTestHelper.Run(source);
        
        // Open generics are not supported for generation yet
        Assert.Empty(diagnostics.Where(d => d.Id == "PlaxionMediator001")); // Should not even try to find a handler for open generic
    }
}

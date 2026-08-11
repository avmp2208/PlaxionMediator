using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using PlaxionMediator.SourceGenerators;

namespace PlaxionMediator.SourceGenerators.Tests;

/// <summary>
/// Validates incremental-generator caching correctness for v0.6.0 model equality work:
/// semantically identical inputs produce identical output, and value-equality models treat
/// location-only differences as unchanged.
/// </summary>
public sealed class GeneratorIncrementalityTests
{
    private const string HandlerSource = """
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

    [Fact]
    public void Second_Run_On_Unchanged_Compilation_Produces_Identical_Output()
    {
        (GeneratorDriver driver, Compilation compilation) = GeneratorTestHelper.CreateDriver(HandlerSource);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation output1, out _);
        GeneratorDriverRunResult first = driver.GetRunResult();

        // Re-run against the same compilation (simulates incremental rebuild with no semantic edits).
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation output2, out _);
        GeneratorDriverRunResult second = driver.GetRunResult();

        Assert.Equal(first.GeneratedTrees.Length, second.GeneratedTrees.Length);
        foreach (SyntaxTree firstTree in first.GeneratedTrees)
        {
            string pathHint = Path.GetFileName(firstTree.FilePath);
            SyntaxTree secondTree = second.GeneratedTrees.First(t =>
                Path.GetFileName(t.FilePath) == pathHint
                || t.FilePath.Contains(Path.GetFileNameWithoutExtension(pathHint), StringComparison.Ordinal));
            Assert.Equal(firstTree.GetText().ToString(), secondTree.GetText().ToString());
        }

        Assert.DoesNotContain(output1.GetDiagnostics(), d => d.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain(output2.GetDiagnostics(), d => d.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void Non_Semantic_Whitespace_Edit_Does_Not_Change_Generated_Source()
    {
        const string edited = """
            using System.Threading;
            using System.Threading.Tasks;
            using PlaxionMediator.Abstractions;

            namespace Demo;

            // trailing whitespace / blank lines shift locations without changing symbols


            public sealed record Ping(string Message) : IRequest<string>;

            public sealed class PingHandler : IRequestHandler<Ping, string>
            {
                public ValueTask<string> Handle(Ping request, CancellationToken cancellationToken)
                    => ValueTask.FromResult("Pong:" + request.Message);
            }
            """;

        (_, _, GeneratorDriverRunResult baseline) = GeneratorTestHelper.Run(HandlerSource);
        (_, _, GeneratorDriverRunResult afterEdit) = GeneratorTestHelper.Run(edited);

        string baselineRegistration = baseline.GeneratedTrees
            .First(t => t.FilePath.Contains("PlaxionMediatorRegistration", StringComparison.Ordinal))
            .GetText()
            .ToString();
        string editedRegistration = afterEdit.GeneratedTrees
            .First(t => t.FilePath.Contains("PlaxionMediatorRegistration", StringComparison.Ordinal))
            .GetText()
            .ToString();
        string baselineSender = baseline.GeneratedTrees
            .First(t => t.FilePath.Contains("PlaxionMediatorSender", StringComparison.Ordinal))
            .GetText()
            .ToString();
        string editedSender = afterEdit.GeneratedTrees
            .First(t => t.FilePath.Contains("PlaxionMediatorSender", StringComparison.Ordinal))
            .GetText()
            .ToString();

        Assert.Equal(baselineRegistration, editedRegistration);
        Assert.Equal(baselineSender, editedSender);
    }

    [Fact]
    public void Model_Equality_Ignores_Location_Only_Differences()
    {
        // Mirrors the generator model contract: location fields must not affect equality/hash,
        // otherwise every line-number shift forces full source re-emission.
        RequestHandlerModel a = new(
            "global::Demo.Ping",
            "string",
            "global::Demo.PingHandler",
            "Ping",
            @"C:\src\Ping.cs",
            RequestLocationLine: 10,
            RequestLocationSpanStart: 100);

        RequestHandlerModel b = new(
            "global::Demo.Ping",
            "string",
            "global::Demo.PingHandler",
            "Ping",
            @"C:\src\Ping.cs",
            RequestLocationLine: 40,
            RequestLocationSpanStart: 400);

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());

        RequestModel ra = new("global::Demo.Ping", "string", "Ping", @"C:\src\Ping.cs", 1, 0);
        RequestModel rb = new("global::Demo.Ping", "string", "Ping", @"C:\src\Ping.cs", 99, 999);
        Assert.Equal(ra, rb);

        EquatableArray<RequestHandlerModel> arrA = new(ImmutableArray.Create(a));
        EquatableArray<RequestHandlerModel> arrB = new(ImmutableArray.Create(b));
        Assert.Equal(arrA, arrB);
        Assert.Equal(arrA.GetHashCode(), arrB.GetHashCode());

        GenerationModel ga = new(arrA, default, default, new EquatableArray<RequestModel>(ImmutableArray.Create(ra)), "Demo");
        GenerationModel gb = new(arrB, default, default, new EquatableArray<RequestModel>(ImmutableArray.Create(rb)), "Demo");
        Assert.Equal(ga, gb);
    }

    [Fact]
    public void Output_Still_Contains_PipelineComposer_ExecuteAsync_For_Empty_Behavior_Path()
    {
        // v0.6.0 routes empty-behavior dispatch through PipelineComposer so observers/extensions fire.
        (_, _, GeneratorDriverRunResult runResult) = GeneratorTestHelper.Run(HandlerSource);
        string sender = runResult.GeneratedTrees
            .First(t => t.FilePath.Contains("PlaxionMediatorSender", StringComparison.Ordinal))
            .GetText()
            .ToString();

        Assert.Contains("PipelineComposer.ExecuteAsync", sender);
        Assert.Contains("PipelineObserverHub.HasObservers", sender);
        Assert.Contains("PipelineExtensionRegistry.HasExtensions", sender);
        Assert.Contains("Array.Empty<IPipelineBehavior<", sender);
    }
}

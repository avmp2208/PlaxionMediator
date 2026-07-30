using PlaxionMediator.Analyzers;
using Microsoft.CodeAnalysis;

namespace PlaxionMediator.Analyzers.Tests;

public sealed class MissingHandlerAnalyzerTests
{
    [Fact]
    public async Task Reports_When_Handler_Missing()
    {
        const string source = """
            using PlaxionMediator.Abstractions;
            public sealed record Q(string X) : IRequest<string>;
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new MissingHandlerAnalyzer(), source);
        Assert.Contains(diagnostics, d => d.Id == "PlaxionMediator001");
    }

    [Fact]
    public async Task No_Diagnostic_When_Handler_Present()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using PlaxionMediator.Abstractions;
            public sealed record Q(string X) : IRequest<string>;
            public sealed class QHandler : IRequestHandler<Q, string>
            {
                public ValueTask<string> Handle(Q request, CancellationToken cancellationToken)
                    => ValueTask.FromResult(request.X);
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new MissingHandlerAnalyzer(), source);
        Assert.DoesNotContain(diagnostics, d => d.Id == "PlaxionMediator001");
    }
}

public sealed class MultipleHandlersAnalyzerTests
{
    [Fact]
    public async Task Reports_When_Multiple_Handlers()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using PlaxionMediator.Abstractions;
            public sealed record Q(string X) : IRequest<string>;
            public sealed class QHandler1 : IRequestHandler<Q, string>
            {
                public ValueTask<string> Handle(Q request, CancellationToken cancellationToken) => ValueTask.FromResult("1");
            }
            public sealed class QHandler2 : IRequestHandler<Q, string>
            {
                public ValueTask<string> Handle(Q request, CancellationToken cancellationToken) => ValueTask.FromResult("2");
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new MultipleHandlersAnalyzer(), source);
        Assert.Contains(diagnostics, d => d.Id == "PlaxionMediator002");
    }
}

public sealed class MutableRequestAnalyzerTests
{
    [Fact]
    public async Task Reports_Mutable_Public_Setters()
    {
        const string source = """
            using PlaxionMediator.Abstractions;
            public sealed class BadRequest : IRequest<string>
            {
                public string Name { get; set; }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new MutableRequestAnalyzer(), source);
        Assert.Contains(diagnostics, d => d.Id == "PlaxionMediator003");
    }

    [Fact]
    public async Task No_Diagnostic_For_Sealed_Record()
    {
        const string source = """
            using PlaxionMediator.Abstractions;
            public sealed record GoodRequest(string Name) : IRequest<string>;
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new MutableRequestAnalyzer(), source);
        Assert.DoesNotContain(diagnostics, d => d.Id == "PlaxionMediator003");
    }

    [Fact]
    public async Task Reports_Mutable_Init_Properties_If_Record_Is_Not_Sealed_Wait_No()
    {
        // Actually our analyzer just looks for 'set' accessors that are not 'init'.
        const string source = """
            using PlaxionMediator.Abstractions;
            public record BadRecord : IRequest<int>
            {
                public int X { get; set; }
            }
            """;
        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new MutableRequestAnalyzer(), source);
        Assert.Contains(diagnostics, d => d.Id == "PlaxionMediator003");
    }
}

public sealed class MissingCancellationTokenAnalyzerTests
{
    [Fact]
    public async Task Reports_When_CancellationToken_Missing()
    {
        // Explicit public Handle without CancellationToken â€” unusual but used to exercise the analyzer.
        // Note: this won't implement the interface correctly, so we use a partial shape:
        // analyzer looks for types implementing IRequestHandler and public Handle methods.
        const string source = """
            using System.Threading.Tasks;
            using PlaxionMediator.Abstractions;
            public sealed record Q(string X) : IRequest<string>;
            public sealed class QHandler : IRequestHandler<Q, string>
            {
                // Explicit interface implementation with token (required to compile)
                ValueTask<string> IRequestHandler<Q, string>.Handle(Q request, System.Threading.CancellationToken cancellationToken)
                    => Handle(request);

                // Public method missing CancellationToken â€” analyzer should flag this
                public ValueTask<string> Handle(Q request) => ValueTask.FromResult(request.X);
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new MissingCancellationTokenAnalyzer(), source);
        Assert.Contains(diagnostics, d => d.Id == "PlaxionMediator004");
    }

    [Fact]
    public async Task No_Diagnostic_When_Token_Present()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using PlaxionMediator.Abstractions;
            public sealed record Q(string X) : IRequest<string>;
            public sealed class QHandler : IRequestHandler<Q, string>
            {
                public ValueTask<string> Handle(Q request, CancellationToken cancellationToken)
                    => ValueTask.FromResult(request.X);
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new MissingCancellationTokenAnalyzer(), source);
        Assert.DoesNotContain(diagnostics, d => d.Id == "PlaxionMediator004");
    }

    [Fact]
    public async Task Reports_Multiple_Issues_In_Same_File()
    {
        const string source = """
            using PlaxionMediator.Abstractions;
            using System.Threading.Tasks;

            public sealed record R1 : IRequest<int>;
            public sealed record R2 : IRequest<int>;

            // PlaxionMediator001 (Missing handler for R2)
            // PlaxionMediator003 (Mutable request R3)
            public class R3 : IRequest<int> { public int X { get; set; } }

            public class R1Handler : IRequestHandler<R1, int>
            {
                // PlaxionMediator004 (Missing token)
                public ValueTask<int> Handle(R1 request) => ValueTask.FromResult(1);
                ValueTask<int> IRequestHandler<R1, int>.Handle(R1 r, System.Threading.CancellationToken ct) => Handle(r);
            }
            """;

        // We check individual analyzers or we could run all, but here we just check if multiple analyzers fire on the same source if we run them.
        var d1 = await AnalyzerTestHelper.GetDiagnosticsAsync(new MissingHandlerAnalyzer(), source);
        Assert.Contains(d1, d => d.Id == "PlaxionMediator001");

        var d3 = await AnalyzerTestHelper.GetDiagnosticsAsync(new MutableRequestAnalyzer(), source);
        Assert.Contains(d3, d => d.Id == "PlaxionMediator003");

        var d4 = await AnalyzerTestHelper.GetDiagnosticsAsync(new MissingCancellationTokenAnalyzer(), source);
        Assert.Contains(d4, d => d.Id == "PlaxionMediator004");
    }
}

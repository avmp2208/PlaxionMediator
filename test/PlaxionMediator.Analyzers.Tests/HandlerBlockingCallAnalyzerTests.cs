using PlaxionMediator.Analyzers;

namespace PlaxionMediator.Analyzers.Tests;

public sealed class HandlerBlockingCallAnalyzerTests
{
    [Fact]
    public async Task Reports_When_Handle_Uses_Result()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using PlaxionMediator.Abstractions;

            public sealed record Q(string X) : IRequest<string>;

            public sealed class QHandler : IRequestHandler<Q, string>
            {
                public ValueTask<string> Handle(Q request, CancellationToken cancellationToken)
                {
                    string value = Task.FromResult(request.X).Result;
                    return ValueTask.FromResult(value);
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(
            new HandlerBlockingCallAnalyzer(),
            source);

        Assert.Contains(diagnostics, d => d.Id == "PlaxionMediator006");
    }

    [Fact]
    public async Task Reports_When_Handle_Uses_Wait()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using PlaxionMediator.Abstractions;

            public sealed record Q(string X) : IRequest<string>;

            public sealed class QHandler : IRequestHandler<Q, string>
            {
                public ValueTask<string> Handle(Q request, CancellationToken cancellationToken)
                {
                    Task.Delay(1).Wait();
                    return ValueTask.FromResult(request.X);
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(
            new HandlerBlockingCallAnalyzer(),
            source);

        Assert.Contains(diagnostics, d => d.Id == "PlaxionMediator006");
    }

    [Fact]
    public async Task Reports_When_Handle_Uses_GetAwaiter_GetResult()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using PlaxionMediator.Abstractions;

            public sealed record Q(string X) : IRequest<string>;

            public sealed class QHandler : IRequestHandler<Q, string>
            {
                public ValueTask<string> Handle(Q request, CancellationToken cancellationToken)
                {
                    string value = Task.FromResult(request.X).GetAwaiter().GetResult();
                    return ValueTask.FromResult(value);
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(
            new HandlerBlockingCallAnalyzer(),
            source);

        Assert.Contains(diagnostics, d => d.Id == "PlaxionMediator006");
    }

    [Fact]
    public async Task No_Diagnostic_When_Handle_Awaits_Properly()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using PlaxionMediator.Abstractions;

            public sealed record Q(string X) : IRequest<string>;

            public sealed class QHandler : IRequestHandler<Q, string>
            {
                public async ValueTask<string> Handle(Q request, CancellationToken cancellationToken)
                {
                    return await Task.FromResult(request.X);
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(
            new HandlerBlockingCallAnalyzer(),
            source);

        Assert.DoesNotContain(diagnostics, d => d.Id == "PlaxionMediator006");
    }

    [Fact]
    public async Task No_Diagnostic_When_Blocking_Call_Is_Outside_Handler()
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

            public static class NotAHandler
            {
                public static string Block() => Task.FromResult("x").Result;
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(
            new HandlerBlockingCallAnalyzer(),
            source);

        Assert.DoesNotContain(diagnostics, d => d.Id == "PlaxionMediator006");
    }
}

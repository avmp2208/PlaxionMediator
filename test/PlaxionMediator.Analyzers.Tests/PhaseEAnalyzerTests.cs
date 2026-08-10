using PlaxionMediator.Analyzers;
using Xunit;

namespace PlaxionMediator.Analyzers.Tests;

public sealed class MultiplePublicConstructorsHandlerAnalyzerTests
{
    [Fact]
    public async Task Reports_When_Handler_Has_Multiple_Public_Constructors()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using PlaxionMediator.Abstractions;
            public sealed record Q(string X) : IRequest<string>;
            public sealed class QHandler : IRequestHandler<Q, string>
            {
                public QHandler() { }
                public QHandler(string s) { }
                public ValueTask<string> Handle(Q request, CancellationToken cancellationToken)
                    => ValueTask.FromResult(request.X);
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new MultiplePublicConstructorsHandlerAnalyzer(), source);
        Assert.Contains(diagnostics, d => d.Id == "PlaxionMediator012");
    }

    [Fact]
    public async Task No_Diagnostic_When_Handler_Has_Single_Public_Constructor()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using PlaxionMediator.Abstractions;
            public sealed record Q(string X) : IRequest<string>;
            public sealed class QHandler : IRequestHandler<Q, string>
            {
                public QHandler(string s) { }
                private QHandler() { }
                public ValueTask<string> Handle(Q request, CancellationToken cancellationToken)
                    => ValueTask.FromResult(request.X);
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new MultiplePublicConstructorsHandlerAnalyzer(), source);
        Assert.DoesNotContain(diagnostics, d => d.Id == "PlaxionMediator012");
    }
}

public sealed class PipelineBehaviorMutableStateAnalyzerTests
{
    [Fact]
    public async Task Reports_When_Behavior_Writes_To_Instance_Field()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using PlaxionMediator.Abstractions;
            public sealed class B<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
                where TRequest : IRequest<TResponse>
            {
                private int _count;
                public ValueTask<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
                {
                    _count++;
                    return next();
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new PipelineBehaviorMutableStateAnalyzer(), source);
        Assert.Contains(diagnostics, d => d.Id == "PlaxionMediator023");
    }

    [Fact]
    public async Task No_Diagnostic_When_Behavior_Stateless()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using PlaxionMediator.Abstractions;
            public sealed class B<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
                where TRequest : IRequest<TResponse>
            {
                public ValueTask<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
                {
                    int local = 0;
                    local++;
                    return next();
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new PipelineBehaviorMutableStateAnalyzer(), source);
        Assert.DoesNotContain(diagnostics, d => d.Id == "PlaxionMediator023");
    }
}

public sealed class LinqUsageInHighFrequencyHandlerAnalyzerTests
{
    [Fact]
    public async Task Reports_When_HighFrequency_Handler_Uses_Linq()
    {
        const string source = """
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;
            using PlaxionMediator.Abstractions;
            [HighFrequency]
            public sealed record Q(List<string> Items) : IRequest<int>;
            public sealed class QHandler : IRequestHandler<Q, int>
            {
                public ValueTask<int> Handle(Q request, CancellationToken cancellationToken)
                {
                    int count = request.Items.Select(x => x).Count();
                    return ValueTask.FromResult(count);
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new LinqUsageInHighFrequencyHandlerAnalyzer(), source);
        Assert.Contains(diagnostics, d => d.Id == "PlaxionMediator050");
    }

    [Fact]
    public async Task No_Diagnostic_When_Normal_Handler_Uses_Linq()
    {
        const string source = """
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;
            using PlaxionMediator.Abstractions;
            public sealed record Q(List<string> Items) : IRequest<int>;
            public sealed class QHandler : IRequestHandler<Q, int>
            {
                public ValueTask<int> Handle(Q request, CancellationToken cancellationToken)
                {
                    int count = request.Items.Select(x => x).Count();
                    return ValueTask.FromResult(count);
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new LinqUsageInHighFrequencyHandlerAnalyzer(), source);
        Assert.DoesNotContain(diagnostics, d => d.Id == "PlaxionMediator050");
    }
}

public sealed class ClosureCaptureInHandlerAnalyzerTests
{
    [Fact]
    public async Task Reports_When_Lambda_Captures_Local()
    {
        const string source = """
            using System;
            using System.Threading;
            using System.Threading.Tasks;
            using PlaxionMediator.Abstractions;
            public sealed record Q(int X) : IRequest<int>;
            public sealed class QHandler : IRequestHandler<Q, int>
            {
                public ValueTask<int> Handle(Q request, CancellationToken cancellationToken)
                {
                    int local = 10;
                    Func<int> f = () => request.X + local;
                    return ValueTask.FromResult(f());
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new ClosureCaptureInHandlerAnalyzer(), source);
        Assert.Contains(diagnostics, d => d.Id == "PlaxionMediator051");
    }

    [Fact]
    public async Task No_Diagnostic_When_No_Capture()
    {
        const string source = """
            using System;
            using System.Threading;
            using System.Threading.Tasks;
            using PlaxionMediator.Abstractions;
            public sealed record Q(int X) : IRequest<int>;
            public sealed class QHandler : IRequestHandler<Q, int>
            {
                public ValueTask<int> Handle(Q request, CancellationToken cancellationToken)
                {
                    Func<int, int> f = x => x * 2;
                    return ValueTask.FromResult(f(request.X));
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new ClosureCaptureInHandlerAnalyzer(), source);
        Assert.DoesNotContain(diagnostics, d => d.Id == "PlaxionMediator051");
    }
}

public sealed class FireAndForgetTaskRunAnalyzerTests
{
    [Fact]
    public async Task Reports_When_TaskRun_Is_Discarded()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using PlaxionMediator.Abstractions;
            public sealed record Q(string X) : IRequest<Unit>;
            public sealed class QHandler : IRequestHandler<Q, Unit>
            {
                public ValueTask<Unit> Handle(Q request, CancellationToken cancellationToken)
                {
                    Task.Run(() => { });
                    return ValueTask.FromResult(Unit.Value);
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new FireAndForgetTaskRunAnalyzer(), source);
        Assert.Contains(diagnostics, d => d.Id == "PlaxionMediator070");
    }

    [Fact]
    public async Task No_Diagnostic_When_TaskRun_Is_Awaited()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using PlaxionMediator.Abstractions;
            public sealed record Q(string X) : IRequest<Unit>;
            public sealed class QHandler : IRequestHandler<Q, Unit>
            {
                public async ValueTask<Unit> Handle(Q request, CancellationToken cancellationToken)
                {
                    await Task.Run(() => { });
                    return Unit.Value;
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new FireAndForgetTaskRunAnalyzer(), source);
        Assert.DoesNotContain(diagnostics, d => d.Id == "PlaxionMediator070");
    }
}

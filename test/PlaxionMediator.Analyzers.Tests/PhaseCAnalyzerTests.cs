using System.Linq;
using PlaxionMediator.Analyzers;

namespace PlaxionMediator.Analyzers.Tests;

public sealed class NonSealedHandlerAnalyzerTests
{
    [Fact]
    public async Task Reports_When_Handler_Is_Not_Sealed()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using PlaxionMediator.Abstractions;
            public sealed record Q(string X) : IRequest<string>;
            public class QHandler : IRequestHandler<Q, string>
            {
                public ValueTask<string> Handle(Q request, CancellationToken cancellationToken)
                    => ValueTask.FromResult(request.X);
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new NonSealedHandlerAnalyzer(), source);
        Assert.Contains(diagnostics, d => d.Id == "PlaxionMediator011");
    }

    [Fact]
    public async Task No_Diagnostic_When_Handler_Is_Sealed()
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

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new NonSealedHandlerAnalyzer(), source);
        Assert.DoesNotContain(diagnostics, d => d.Id == "PlaxionMediator011");
    }
}

public sealed class InvalidBehaviorRegistrationAnalyzerTests
{
    [Fact]
    public async Task Reports_When_Use_Type_Is_Not_Behavior()
    {
        const string source = """
            using PlaxionMediator.Pipeline;
            public sealed class NotABehavior { }
            public static class C {
                public static void M() {
                    var b = new PipelineBuilder();
                    b.Use<NotABehavior>();
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new InvalidBehaviorRegistrationAnalyzer(), source);
        Assert.Contains(diagnostics, d => d.Id == "PlaxionMediator020");
    }

    [Fact]
    public async Task No_Diagnostic_When_Use_Type_Is_Behavior()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using PlaxionMediator.Abstractions;
            using PlaxionMediator.Pipeline;
            public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
                where TRequest : IRequest<TResponse>
            {
                public ValueTask<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
                    => next();
            }
            public static class C {
                public static void M() {
                    var b = new PipelineBuilder();
                    b.Use<LoggingBehavior<IRequest<int>, int>>();
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new InvalidBehaviorRegistrationAnalyzer(), source);
        Assert.DoesNotContain(diagnostics, d => d.Id == "PlaxionMediator020");
    }
}

public sealed class DuplicateRegistrationAnalyzerTests
{
    [Fact]
    public async Task Reports_When_Same_Behavior_Registered_Twice()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using PlaxionMediator.Abstractions;
            using PlaxionMediator.Pipeline;
            public sealed class B<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
                where TRequest : IRequest<TResponse>
            {
                public ValueTask<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
                    => next();
            }
            public static class C {
                public static void M() {
                    new PipelineBuilder()
                        .Use<B<IRequest<int>, int>>()
                        .Use<B<IRequest<int>, int>>();
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new DuplicateRegistrationAnalyzer(), source);
        Assert.Contains(diagnostics, d => d.Id == "PlaxionMediator021");
    }

    [Fact]
    public async Task No_Diagnostic_When_Behaviors_Differ()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using PlaxionMediator.Abstractions;
            using PlaxionMediator.Pipeline;
            public sealed class A<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
                where TRequest : IRequest<TResponse>
            {
                public ValueTask<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
                    => next();
            }
            public sealed class B<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
                where TRequest : IRequest<TResponse>
            {
                public ValueTask<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
                    => next();
            }
            public static class C {
                public static void M() {
                    new PipelineBuilder()
                        .Use<A<IRequest<int>, int>>()
                        .Use<B<IRequest<int>, int>>();
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new DuplicateRegistrationAnalyzer(), source);
        Assert.DoesNotContain(diagnostics, d => d.Id == "PlaxionMediator021");
    }
}

public sealed class IncorrectLifetimeAnalyzerTests
{
    [Fact]
    public async Task Reports_When_Singleton_Handler_Captures_Scoped_Dependency()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using Microsoft.Extensions.DependencyInjection;
            using PlaxionMediator.Abstractions;

            public sealed class ScopedDep { }
            public sealed record Q(string X) : IRequest<string>;
            public sealed class QHandler : IRequestHandler<Q, string>
            {
                private readonly ScopedDep _dep;
                public QHandler(ScopedDep dep) => _dep = dep;
                public ValueTask<string> Handle(Q request, CancellationToken cancellationToken)
                    => ValueTask.FromResult(request.X);
            }
            public static class Reg {
                public static void M(IServiceCollection services) {
                    services.AddScoped<ScopedDep>();
                    services.AddSingleton<QHandler>();
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new IncorrectLifetimeAnalyzer(), source);
        Assert.Contains(diagnostics, d => d.Id == "PlaxionMediator022");
    }

    [Fact]
    public async Task No_Diagnostic_When_Handler_Is_Scoped()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using Microsoft.Extensions.DependencyInjection;
            using PlaxionMediator.Abstractions;

            public sealed class ScopedDep { }
            public sealed record Q(string X) : IRequest<string>;
            public sealed class QHandler : IRequestHandler<Q, string>
            {
                public QHandler(ScopedDep dep) { }
                public ValueTask<string> Handle(Q request, CancellationToken cancellationToken)
                    => ValueTask.FromResult(request.X);
            }
            public static class Reg {
                public static void M(IServiceCollection services) {
                    services.AddScoped<ScopedDep>();
                    services.AddScoped<QHandler>();
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new IncorrectLifetimeAnalyzer(), source);
        Assert.DoesNotContain(diagnostics, d => d.Id == "PlaxionMediator022");
    }

    [Fact]
    public async Task No_Diagnostic_When_Ctor_Parameter_Is_Not_Captured()
    {
        // False positive: the constructor accepts a Scoped dependency but never stores it
        // anywhere (e.g. only reads a value out of it during construction). No field/property
        // retains a reference, so the Singleton cannot actually leak the shorter-lived dependency.
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using Microsoft.Extensions.DependencyInjection;
            using PlaxionMediator.Abstractions;

            public sealed class ScopedDep { public void Touch() { } }
            public sealed record Q(string X) : IRequest<string>;
            public sealed class QHandler : IRequestHandler<Q, string>
            {
                public QHandler(ScopedDep dep)
                {
                    dep.Touch();
                }
                public ValueTask<string> Handle(Q request, CancellationToken cancellationToken)
                    => ValueTask.FromResult(request.X);
            }
            public static class Reg {
                public static void M(IServiceCollection services) {
                    services.AddScoped<ScopedDep>();
                    services.AddSingleton<QHandler>();
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new IncorrectLifetimeAnalyzer(), source);
        Assert.DoesNotContain(diagnostics, d => d.Id == "PlaxionMediator022");
    }

    [Fact]
    public async Task No_Diagnostic_When_Primary_Constructor_Parameter_Is_Unused()
    {
        // False positive: primary constructor declares a Scoped dependency parameter that is
        // never referenced anywhere in the type, so the compiler does not synthesize a capturing
        // field for it (CS9113 unused parameter) — there is no actual lifetime leak.
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using Microsoft.Extensions.DependencyInjection;
            using PlaxionMediator.Abstractions;

            public sealed class ScopedDep { }
            public sealed record Q(string X) : IRequest<string>;
            public sealed class QHandler(ScopedDep dep) : IRequestHandler<Q, string>
            {
                public ValueTask<string> Handle(Q request, CancellationToken cancellationToken)
                    => ValueTask.FromResult(request.X);
            }
            public static class Reg {
                public static void M(IServiceCollection services) {
                    services.AddScoped<ScopedDep>();
                    services.AddSingleton<QHandler>();
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new IncorrectLifetimeAnalyzer(), source);
        Assert.DoesNotContain(diagnostics, d => d.Id == "PlaxionMediator022");
    }

    [Fact]
    public async Task Reports_When_Primary_Constructor_Parameter_Is_Captured()
    {
        // True positive must still fire: primary constructor parameter is referenced in the
        // Handle method body, so the compiler synthesizes a capturing backing field.
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using Microsoft.Extensions.DependencyInjection;
            using PlaxionMediator.Abstractions;

            public sealed class ScopedDep { public string Value => "v"; }
            public sealed record Q(string X) : IRequest<string>;
            public sealed class QHandler(ScopedDep dep) : IRequestHandler<Q, string>
            {
                public ValueTask<string> Handle(Q request, CancellationToken cancellationToken)
                    => ValueTask.FromResult(dep.Value);
            }
            public static class Reg {
                public static void M(IServiceCollection services) {
                    services.AddScoped<ScopedDep>();
                    services.AddSingleton<QHandler>();
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new IncorrectLifetimeAnalyzer(), source);
        Assert.Contains(diagnostics, d => d.Id == "PlaxionMediator022");
    }
}

public sealed class MissingCancellationTokenPropagationAnalyzerTests
{
    [Fact]
    public async Task Reports_When_Token_Not_Propagated()
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
                    await Task.Delay(1);
                    return request.X;
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new MissingCancellationTokenPropagationAnalyzer(), source);
        Assert.Contains(diagnostics, d => d.Id == "PlaxionMediator031");
    }

    [Fact]
    public async Task No_Diagnostic_When_Token_Propagated()
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
                    await Task.Delay(1, cancellationToken);
                    return request.X;
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new MissingCancellationTokenPropagationAnalyzer(), source);
        Assert.DoesNotContain(diagnostics, d => d.Id == "PlaxionMediator031");
    }

    [Fact]
    public async Task No_Diagnostic_When_Local_Function_Correctly_Forwards_Captured_Ambient_Token()
    {
        // False positive guard: the local function closes over the ambient token and forwards it
        // correctly; neither the call to the local function nor the call inside it should be flagged.
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using PlaxionMediator.Abstractions;
            public sealed record Q(string X) : IRequest<string>;
            public sealed class QHandler : IRequestHandler<Q, string>
            {
                public async ValueTask<string> Handle(Q request, CancellationToken cancellationToken)
                {
                    async Task<string> Inner()
                    {
                        await Task.Delay(1, cancellationToken);
                        return request.X;
                    }
                    return await Inner();
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new MissingCancellationTokenPropagationAnalyzer(), source);
        Assert.DoesNotContain(diagnostics, d => d.Id == "PlaxionMediator031");
    }

    [Fact]
    public async Task Reports_When_Local_Function_Receives_Token_But_Fails_To_Forward_It()
    {
        // True positive: the ambient token is passed into the local function, but the local
        // function itself drops it on the awaited call — this genuine violation must still fire.
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using PlaxionMediator.Abstractions;
            public sealed record Q(string X) : IRequest<string>;
            public sealed class QHandler : IRequestHandler<Q, string>
            {
                public async ValueTask<string> Handle(Q request, CancellationToken cancellationToken)
                {
                    async Task<string> Inner(CancellationToken ct)
                    {
                        await Task.Delay(1);
                        return request.X;
                    }
                    return await Inner(cancellationToken);
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new MissingCancellationTokenPropagationAnalyzer(), source);
        Assert.Contains(diagnostics, d => d.Id == "PlaxionMediator031");
    }

    [Fact]
    public async Task No_Diagnostic_When_Static_Local_Function_Cannot_Access_Ambient_Token()
    {
        // False positive guard: a static local function cannot capture the ambient token at all,
        // so it must not be flagged for "failing" to forward a token it has no access to.
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using PlaxionMediator.Abstractions;
            public sealed record Q(string X) : IRequest<string>;
            public sealed class QHandler : IRequestHandler<Q, string>
            {
                public async ValueTask<string> Handle(Q request, CancellationToken cancellationToken)
                {
                    return await Inner(request.X);

                    static async Task<string> Inner(string x)
                    {
                        await Task.Delay(1);
                        return x;
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new MissingCancellationTokenPropagationAnalyzer(), source);
        Assert.DoesNotContain(diagnostics, d => d.Id == "PlaxionMediator031");
    }
}

public sealed class CancellationTokenNoneAnalyzerTests
{
    [Fact]
    public async Task Reports_When_None_Used_With_Ambient_Token()
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
                    await Task.Delay(1, CancellationToken.None);
                    return request.X;
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new CancellationTokenNoneAnalyzer(), source);
        Assert.Contains(diagnostics, d => d.Id == "PlaxionMediator032");
    }

    [Fact]
    public async Task No_Diagnostic_When_Ambient_Token_Used()
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
                    await Task.Delay(1, cancellationToken);
                    return request.X;
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new CancellationTokenNoneAnalyzer(), source);
        Assert.DoesNotContain(diagnostics, d => d.Id == "PlaxionMediator032");
    }

    [Fact]
    public async Task Reports_When_Local_Function_Uses_None_Despite_Captured_Ambient_Token()
    {
        // True positive across a local function: the ambient token is reachable via closure, so
        // using CancellationToken.None inside the local function is still a genuine violation.
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using PlaxionMediator.Abstractions;
            public sealed record Q(string X) : IRequest<string>;
            public sealed class QHandler : IRequestHandler<Q, string>
            {
                public async ValueTask<string> Handle(Q request, CancellationToken cancellationToken)
                {
                    async Task<string> Inner()
                    {
                        await Task.Delay(1, CancellationToken.None);
                        return request.X;
                    }
                    return await Inner();
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new CancellationTokenNoneAnalyzer(), source);
        Assert.Contains(diagnostics, d => d.Id == "PlaxionMediator032");
    }

    [Fact]
    public async Task No_Diagnostic_When_Static_Local_Function_Uses_None_With_No_Ambient_Access()
    {
        // False positive guard: a static local function cannot capture the ambient token at all,
        // so using CancellationToken.None there is the only option and must not be flagged.
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using PlaxionMediator.Abstractions;
            public sealed record Q(string X) : IRequest<string>;
            public sealed class QHandler : IRequestHandler<Q, string>
            {
                public async ValueTask<string> Handle(Q request, CancellationToken cancellationToken)
                {
                    return await Inner(request.X);

                    static async Task<string> Inner(string x)
                    {
                        await Task.Delay(1, CancellationToken.None);
                        return x;
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new CancellationTokenNoneAnalyzer(), source);
        Assert.DoesNotContain(diagnostics, d => d.Id == "PlaxionMediator032");
    }
}

public sealed class AsyncVoidHandlerAnalyzerTests
{
    [Fact]
    public async Task Reports_Async_Void_Handle()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using PlaxionMediator.Abstractions;
            public sealed record N(string X) : INotification;
            public sealed class NHandler : INotificationHandler<N>
            {
                ValueTask INotificationHandler<N>.Handle(N notification, CancellationToken cancellationToken)
                {
                    Handle(notification, cancellationToken);
                    return default;
                }
                public async void Handle(N notification, CancellationToken cancellationToken)
                {
                    await Task.Yield();
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new AsyncVoidHandlerAnalyzer(), source);
        Assert.Contains(diagnostics, d => d.Id == "PlaxionMediator040");
    }

    [Fact]
    public async Task No_Diagnostic_For_ValueTask_Handle()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using PlaxionMediator.Abstractions;
            public sealed record N(string X) : INotification;
            public sealed class NHandler : INotificationHandler<N>
            {
                public ValueTask Handle(N notification, CancellationToken cancellationToken) => default;
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new AsyncVoidHandlerAnalyzer(), source);
        Assert.DoesNotContain(diagnostics, d => d.Id == "PlaxionMediator040");
    }
}

public sealed class HandlerSelfSendAnalyzerTests
{
    [Fact]
    public async Task Reports_When_Handler_Sends_Own_Request()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using PlaxionMediator.Abstractions;
            using PlaxionMediator.Core;
            public sealed record Q(string X) : IRequest<string>;
            public sealed class QHandler : IRequestHandler<Q, string>
            {
                private readonly ISender _sender;
                public QHandler(ISender sender) => _sender = sender;
                public async ValueTask<string> Handle(Q request, CancellationToken cancellationToken)
                {
                    return await _sender.Send(new Q(request.X), cancellationToken);
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new HandlerSelfSendAnalyzer(), source);
        Assert.Contains(diagnostics, d => d.Id == "PlaxionMediator041");
    }

    [Fact]
    public async Task No_Diagnostic_When_Sending_Other_Request()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using PlaxionMediator.Abstractions;
            using PlaxionMediator.Core;
            public sealed record Q(string X) : IRequest<string>;
            public sealed record Other(string X) : IRequest<string>;
            public sealed class QHandler : IRequestHandler<Q, string>
            {
                private readonly ISender _sender;
                public QHandler(ISender sender) => _sender = sender;
                public async ValueTask<string> Handle(Q request, CancellationToken cancellationToken)
                {
                    return await _sender.Send(new Other(request.X), cancellationToken);
                }
            }
            public sealed class OtherHandler : IRequestHandler<Other, string>
            {
                public ValueTask<string> Handle(Other request, CancellationToken cancellationToken)
                    => ValueTask.FromResult(request.X);
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new HandlerSelfSendAnalyzer(), source);
        Assert.DoesNotContain(diagnostics, d => d.Id == "PlaxionMediator041");
    }
}

public sealed class UnnecessaryBehaviorOnHotPathAnalyzerTests
{
    [Fact]
    public async Task Reports_When_Too_Many_Behaviors_On_HighFrequency_Request()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using Microsoft.Extensions.DependencyInjection;
            using PlaxionMediator.Abstractions;

            [HighFrequency]
            public sealed record Hot(string X) : IRequest<string>;
            public sealed class HotHandler : IRequestHandler<Hot, string>
            {
                public ValueTask<string> Handle(Hot request, CancellationToken cancellationToken)
                    => ValueTask.FromResult(request.X);
            }
            public sealed class B1<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
            {
                public ValueTask<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken) => next();
            }
            public sealed class B2<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
            {
                public ValueTask<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken) => next();
            }
            public sealed class B3<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
            {
                public ValueTask<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken) => next();
            }
            public sealed class B4<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
            {
                public ValueTask<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken) => next();
            }
            public static class Reg {
                public static void M(IServiceCollection s) {
                    s.AddSingleton(typeof(IPipelineBehavior<,>), typeof(B1<,>));
                    s.AddSingleton(typeof(IPipelineBehavior<,>), typeof(B2<,>));
                    s.AddSingleton(typeof(IPipelineBehavior<,>), typeof(B3<,>));
                    s.AddSingleton(typeof(IPipelineBehavior<,>), typeof(B4<,>));
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new UnnecessaryBehaviorOnHotPathAnalyzer(), source);
        Assert.Contains(diagnostics, d => d.Id == "PlaxionMediator080");
    }

    [Fact]
    public async Task No_Diagnostic_Without_HighFrequency()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using Microsoft.Extensions.DependencyInjection;
            using PlaxionMediator.Abstractions;

            public sealed record Cold(string X) : IRequest<string>;
            public sealed class ColdHandler : IRequestHandler<Cold, string>
            {
                public ValueTask<string> Handle(Cold request, CancellationToken cancellationToken)
                    => ValueTask.FromResult(request.X);
            }
            public sealed class B1<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
            {
                public ValueTask<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken) => next();
            }
            public sealed class B2<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
            {
                public ValueTask<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken) => next();
            }
            public sealed class B3<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
            {
                public ValueTask<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken) => next();
            }
            public sealed class B4<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
            {
                public ValueTask<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken) => next();
            }
            public static class Reg {
                public static void M(IServiceCollection s) {
                    s.AddSingleton(typeof(IPipelineBehavior<,>), typeof(B1<,>));
                    s.AddSingleton(typeof(IPipelineBehavior<,>), typeof(B2<,>));
                    s.AddSingleton(typeof(IPipelineBehavior<,>), typeof(B3<,>));
                    s.AddSingleton(typeof(IPipelineBehavior<,>), typeof(B4<,>));
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new UnnecessaryBehaviorOnHotPathAnalyzer(), source);
        Assert.DoesNotContain(diagnostics, d => d.Id == "PlaxionMediator080");
    }
}

public sealed class SynchronousOnlyHandlerAnalyzerTests
{
    [Fact]
    public async Task Reports_When_No_Await()
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
                    return ValueTask.FromResult(request.X);
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new SynchronousOnlyHandlerAnalyzer(), source);
        Assert.Contains(diagnostics, d => d.Id == "PlaxionMediator081");
    }

    [Fact]
    public async Task No_Diagnostic_When_Awaits()
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
                    await Task.Yield();
                    return request.X;
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new SynchronousOnlyHandlerAnalyzer(), source);
        Assert.DoesNotContain(diagnostics, d => d.Id == "PlaxionMediator081");
    }
}

public sealed class BehaviorAllocatesInHotPathAnalyzerTests
{
    [Fact]
    public async Task Reports_When_Behavior_Allocates_List()
    {
        const string source = """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using PlaxionMediator.Abstractions;
            public sealed class AllocBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
                where TRequest : IRequest<TResponse>
            {
                public ValueTask<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
                {
                    var list = new List<string>();
                    list.Add("x");
                    return next();
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new BehaviorAllocatesInHotPathAnalyzer(), source);
        Assert.Contains(diagnostics, d => d.Id == "PlaxionMediator082");
    }

    [Fact]
    public async Task No_Diagnostic_When_No_Allocation()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using PlaxionMediator.Abstractions;
            public sealed class CleanBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
                where TRequest : IRequest<TResponse>
            {
                public ValueTask<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
                    => next();
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new BehaviorAllocatesInHotPathAnalyzer(), source);
        Assert.DoesNotContain(diagnostics, d => d.Id == "PlaxionMediator082");
    }
}

public sealed class StreamHandlerBuffersAnalyzerTests
{
    [Fact]
    public async Task Reports_When_Stream_Handler_Uses_ToList()
    {
        const string source = """
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading;
            using System.Threading.Tasks;
            using PlaxionMediator.Abstractions;
            public sealed record StreamQ(int Count) : IStreamRequest<int>;
            public sealed class StreamQHandler : IStreamRequestHandler<StreamQ, int>
            {
                public async IAsyncEnumerable<int> Handle(StreamQ request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
                {
                    var buffered = Enumerable.Range(0, request.Count).ToList();
                    foreach (var item in buffered)
                    {
                        yield return item;
                    }
                    await Task.CompletedTask;
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new StreamHandlerBuffersAnalyzer(), source);
        Assert.Contains(diagnostics, d => d.Id == "PlaxionMediator083");
    }

    [Fact]
    public async Task No_Diagnostic_When_Yielding_Incrementally()
    {
        const string source = """
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using PlaxionMediator.Abstractions;
            public sealed record StreamQ(int Count) : IStreamRequest<int>;
            public sealed class StreamQHandler : IStreamRequestHandler<StreamQ, int>
            {
                public async IAsyncEnumerable<int> Handle(StreamQ request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
                {
                    for (int i = 0; i < request.Count; i++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        yield return i;
                    }
                    await Task.CompletedTask;
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new StreamHandlerBuffersAnalyzer(), source);
        Assert.DoesNotContain(diagnostics, d => d.Id == "PlaxionMediator083");
    }
}

public sealed class NotificationHandlerThrowsAnalyzerTests
{
    [Fact]
    public async Task Reports_When_Catch_Only_Rethrows()
    {
        const string source = """
            using System;
            using System.Threading;
            using System.Threading.Tasks;
            using PlaxionMediator.Abstractions;
            public sealed record N(string X) : INotification;
            public sealed class NHandler : INotificationHandler<N>
            {
                public async ValueTask Handle(N notification, CancellationToken cancellationToken)
                {
                    try
                    {
                        await Task.Yield();
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new NotificationHandlerThrowsAnalyzer(), source);
        Assert.Contains(diagnostics, d => d.Id == "PlaxionMediator090");
    }

    [Fact]
    public async Task No_Diagnostic_When_No_Fail_Fast_Catch()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using PlaxionMediator.Abstractions;
            public sealed record N(string X) : INotification;
            public sealed class NHandler : INotificationHandler<N>
            {
                public ValueTask Handle(N notification, CancellationToken cancellationToken)
                    => ValueTask.CompletedTask;
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new NotificationHandlerThrowsAnalyzer(), source);
        Assert.DoesNotContain(diagnostics, d => d.Id == "PlaxionMediator090");
    }
}

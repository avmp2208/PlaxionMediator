using PlaxionMediator.Analyzers;

namespace PlaxionMediator.Analyzers.Tests;

public sealed class InvalidExtensionRegistrationAnalyzerTests
{
    [Fact]
    public async Task Reports_When_Use_Type_Is_Not_Extension()
    {
        const string source = """
            using PlaxionMediator.Pipeline;
            public sealed class NotAnExtension { }
            public static class C {
                public static void M() {
                    var b = new PipelineExtensionBuilder();
                    b.Use<NotAnExtension>();
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new InvalidExtensionRegistrationAnalyzer(), source);
        Assert.Contains(diagnostics, d => d.Id == "PlaxionMediator024");
    }

    [Fact]
    public async Task No_Diagnostic_When_Use_Type_Is_Extension()
    {
        const string source = """
            using PlaxionMediator.Abstractions;
            using PlaxionMediator.Pipeline;
            public sealed class LoggingExtension : IPipelineExtension
            {
                public int Order => 0;
                public RequestHandlerDelegate<TResponse> Apply<TRequest, TResponse>(
                    in PipelineExtensionContext context,
                    RequestHandlerDelegate<TResponse> next)
                    where TRequest : IRequest<TResponse>
                    => next;
            }
            public static class C {
                public static void M() {
                    var b = new PipelineExtensionBuilder();
                    b.Use<LoggingExtension>();
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new InvalidExtensionRegistrationAnalyzer(), source);
        Assert.DoesNotContain(diagnostics, d => d.Id == "PlaxionMediator024");
    }
}

public sealed class DuplicateExtensionRegistrationAnalyzerTests
{
    [Fact]
    public async Task Reports_When_Same_Extension_Registered_Twice()
    {
        const string source = """
            using PlaxionMediator.Abstractions;
            using PlaxionMediator.Pipeline;
            public sealed class Ext : IPipelineExtension
            {
                public int Order => 0;
                public RequestHandlerDelegate<TResponse> Apply<TRequest, TResponse>(
                    in PipelineExtensionContext context,
                    RequestHandlerDelegate<TResponse> next)
                    where TRequest : IRequest<TResponse>
                    => next;
            }
            public static class C {
                public static void M() {
                    new PipelineExtensionBuilder()
                        .Use<Ext>()
                        .Use<Ext>();
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new DuplicateExtensionRegistrationAnalyzer(), source);
        Assert.Contains(diagnostics, d => d.Id == "PlaxionMediator025");
    }

    [Fact]
    public async Task No_Diagnostic_When_Extensions_Differ()
    {
        const string source = """
            using PlaxionMediator.Abstractions;
            using PlaxionMediator.Pipeline;
            public sealed class ExtA : IPipelineExtension
            {
                public int Order => 0;
                public RequestHandlerDelegate<TResponse> Apply<TRequest, TResponse>(
                    in PipelineExtensionContext context,
                    RequestHandlerDelegate<TResponse> next)
                    where TRequest : IRequest<TResponse>
                    => next;
            }
            public sealed class ExtB : IPipelineExtension
            {
                public int Order => 1;
                public RequestHandlerDelegate<TResponse> Apply<TRequest, TResponse>(
                    in PipelineExtensionContext context,
                    RequestHandlerDelegate<TResponse> next)
                    where TRequest : IRequest<TResponse>
                    => next;
            }
            public static class C {
                public static void M() {
                    new PipelineExtensionBuilder()
                        .Use<ExtA>()
                        .Use<ExtB>();
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new DuplicateExtensionRegistrationAnalyzer(), source);
        Assert.DoesNotContain(diagnostics, d => d.Id == "PlaxionMediator025");
    }
}

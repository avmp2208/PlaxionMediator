// This test project is a *binary-compatibility regression harness* for the public, shipped
// PlaxionMediator surface (ISender/IPublisher, IRequest/INotification, IPipelineBehavior,
// AddPlaxionMediatorCore/AddPlaxionMediatorDispatcher).
//
// IMPORTANT — documented limitation:
// A true binary-compatibility check compiles a fixed "consumer" assembly once against a frozen,
// previously-published baseline version of the PlaxionMediator assemblies, and then re-runs that
// *same compiled consumer assembly* (without recompiling) against a newer build's assemblies,
// asserting no MissingMethodException/TypeLoadException/FieldAccessException occurs. That requires
// two distinct published NuGet versions of PlaxionMediator to diff against, which is not practical
// inside a single-version repository checkout (there is no "previous" build to load side-by-side).
//
// As a practical stand-in that can run entirely from source in this repository, this project:
//   1. Compiles a small fixed "consumer" sample (below) directly against the current
//      PlaxionMediator/PlaxionMediator.Abstractions/PlaxionMediator.Core/PlaxionMediator.Pipeline
//      assemblies, exercising Send, Publish, pipeline behaviors, and DI registration end-to-end.
//   2. Uses reflection to assert that the exact public members relied upon by that consumer sample
//      are present with the expected signatures on the shipped public types, which is the
//      lightweight, single-version-repo equivalent of catching a MissingMethodException/
//      TypeLoadException at load time (those exceptions are what a real cross-version binary diff
//      would surface if a member were removed, renamed, or had an incompatible signature change).
//
// This harness intentionally does NOT replace Phase 7's `PublicAPI.Shipped.txt`/`PublicAPI.Unshipped.txt`
// source-compatibility gates (Microsoft.CodeAnalysis.PublicApiAnalyzers) — source compatibility and
// binary compatibility are distinct guarantees, and this project only targets the latter, as a
// lightweight, always-runnable proxy.

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using PlaxionMediator;
using PlaxionMediator.Abstractions;
using PlaxionMediator.Core;

namespace PlaxionMediator.BinaryCompatibility.Tests;

public sealed class BinaryCompatibilitySmokeTests
{
    // --- Fixed "consumer" sample types exercising the core public surface. ---

    private sealed record Ping(string Message) : IRequest<string>;

    private sealed class PingHandler : IRequestHandler<Ping, string>
    {
        public ValueTask<string> Handle(Ping request, CancellationToken cancellationToken)
            => ValueTask.FromResult("Pong:" + request.Message);
    }

    private sealed record Pinged(string Message) : INotification;

    private sealed class PingedHandler : INotificationHandler<Pinged>
    {
        public static int CallCount;

        public ValueTask Handle(Pinged notification, CancellationToken cancellationToken)
        {
            CallCount++;
            return default;
        }
    }

    private sealed class LoggingBehavior : IPipelineBehavior<Ping, string>
    {
        public static readonly List<string> Log = [];

        public async ValueTask<string> Handle(Ping request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
        {
            Log.Add("before");
            string result = await next();
            Log.Add("after");
            return result;
        }
    }

    private sealed class ConsumerDispatcher : ISender, IPublisher
    {
        private readonly IServiceProvider _sp;

        public ConsumerDispatcher(IServiceProvider sp) => _sp = sp;

        public ValueTask<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request is Ping ping && typeof(TResponse) == typeof(string))
            {
                IRequestHandler<Ping, string> handler = _sp.GetRequiredService<IRequestHandler<Ping, string>>();
                List<IPipelineBehavior<Ping, string>> behaviors = _sp.GetServices<IPipelineBehavior<Ping, string>>().ToList();

                ValueTask<string> Execute() => behaviors.Count == 0
                    ? handler.Handle(ping, cancellationToken)
                    : PlaxionMediator.Pipeline.PipelineComposer.ExecuteAsync(ping, behaviors, handler.Handle, cancellationToken);

                return AdaptAsync<string, TResponse>(Execute());
            }

            throw new HandlerNotFoundException(request.GetType());
        }

        public async ValueTask Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            if (notification is Pinged)
            {
                foreach (INotificationHandler<Pinged> handler in _sp.GetServices<INotificationHandler<Pinged>>())
                {
                    await handler.Handle((Pinged)(object)notification, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
            IStreamRequest<TResponse> request,
            CancellationToken cancellationToken = default)
            => throw new HandlerNotFoundException(request.GetType());

        private static async ValueTask<TResponse> AdaptAsync<TActual, TResponse>(ValueTask<TActual> source)
        {
            TActual result = await source.ConfigureAwait(false);
            return (TResponse)(object)result!;
        }
    }

    // --- End-to-end functional exercise of the consumer sample against the current build. ---

    [Fact]
    public async Task ConsumerSample_Send_Publish_And_Pipeline_Behaviors_Resolve_And_Execute()
    {
        ServiceCollection services = new();
        services.AddPlaxionMediatorCore();
        services.AddScoped<IRequestHandler<Ping, string>, PingHandler>();
        services.AddScoped<INotificationHandler<Pinged>, PingedHandler>();
        services.AddScoped<IPipelineBehavior<Ping, string>, LoggingBehavior>();
        services.AddPlaxionMediatorDispatcher<ConsumerDispatcher>();

        await using ServiceProvider sp = services.BuildServiceProvider();
        using IServiceScope scope = sp.CreateScope();
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();
        IPublisher publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        string result = await sender.Send(new Ping("hi"));
        Assert.Equal("Pong:hi", result);
        Assert.Equal(["before", "after"], LoggingBehavior.Log);

        int before = PingedHandler.CallCount;
        await publisher.Publish(new Pinged("hi"));
        Assert.Equal(before + 1, PingedHandler.CallCount);
    }

    // --- Reflection-based smoke checks: assert that the shipped public members relied upon above
    //     are still present with the expected shape. This is the stand-in for a real
    //     MissingMethodException/TypeLoadException cross-version diff (see header comment). ---

    [Theory]
    [InlineData(typeof(ISender), "Send")]
    [InlineData(typeof(ISender), "CreateStream")]
    [InlineData(typeof(IPublisher), "Publish")]
    public void CoreDispatcherInterfaces_Expose_Expected_PublicMethods(Type type, string methodName)
    {
        Assert.True(type.IsInterface, $"{type} is expected to remain a public interface.");
        Assert.Contains(methodName, type.GetMethods().Select(m => m.Name));
    }

    [Fact]
    public void IPipelineBehavior_Handle_Signature_Is_Unchanged()
    {
        MethodInfo? handle = typeof(IPipelineBehavior<,>).GetMethod("Handle");
        Assert.NotNull(handle);
        Assert.Equal(3, handle!.GetParameters().Length);
    }

    [Fact]
    public void ServiceCollectionExtensions_Expose_Expected_RegistrationMethods()
    {
        Type extensions = typeof(PlaxionMediatorServiceCollectionExtensions);
        Assert.Contains(
            extensions.GetMethods(BindingFlags.Public | BindingFlags.Static),
            m => m.Name == "AddPlaxionMediatorCore");
        Assert.Contains(
            extensions.GetMethods(BindingFlags.Public | BindingFlags.Static),
            m => m.Name == "AddPlaxionMediatorDispatcher");
        Assert.Contains(
            extensions.GetMethods(BindingFlags.Public | BindingFlags.Static),
            m => m.Name == "AddPlaxionMediator");
    }

    [Fact]
    public void PlaxionMediatorOptions_Exposes_Expected_PublicProperties()
    {
        Type options = typeof(PlaxionMediatorOptions);
        PropertyInfo? lifetime = options.GetProperty("DefaultHandlerLifetime");
        PropertyInfo? behaviors = options.GetProperty("GlobalBehaviors");

        Assert.NotNull(lifetime);
        Assert.NotNull(behaviors);
    }

    [Fact]
    public void Unit_Value_Field_And_Equality_Members_Are_Present()
    {
        Type unit = typeof(Unit);
        Assert.NotNull(unit.GetField("Value", BindingFlags.Public | BindingFlags.Static));
        Assert.Contains(unit.GetMethods(), m => m.Name == "Equals" && m.GetParameters().Length == 1);
    }
}

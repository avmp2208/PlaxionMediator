using PlaxionMediator.Abstractions;
using PlaxionMediator.Core;
using PlaxionMediator.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace PlaxionMediator.DependencyInjection.Tests;

public sealed class AddPlaxionMediatorTests
{
    private sealed record Ping(string Message) : IRequest<string>;

    private sealed class PingHandler : IRequestHandler<Ping, string>
    {
        private readonly List<string>? _log;
        public PingHandler(List<string>? log = null) => _log = log;

        public ValueTask<string> Handle(Ping request, CancellationToken cancellationToken)
        {
            _log?.Add("Handler");
            return ValueTask.FromResult("Pong:" + request.Message);
        }
    }

    private sealed class TestDispatcher : ISender, IPublisher
    {
        private readonly IServiceProvider _sp;

        public TestDispatcher(IServiceProvider sp) => _sp = sp;

        public ValueTask<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request is Ping ping && typeof(TResponse) == typeof(string))
            {
                IRequestHandler<Ping, string> handler = _sp.GetRequiredService<IRequestHandler<Ping, string>>();
                var behaviors = _sp.GetServices<IPipelineBehavior<Ping, string>>().ToList();
                
                if (behaviors.Count == 0)
                {
                    return Adapt<string, TResponse>(handler.Handle(ping, cancellationToken));
                }
                
                return Adapt<string, TResponse>(PlaxionMediator.Pipeline.PipelineComposer.ExecuteAsync(ping, behaviors, handler.Handle, cancellationToken));
            }

            throw new HandlerNotFoundException(request.GetType());
        }

        public ValueTask Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
            => default;

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
            IStreamRequest<TResponse> request,
            CancellationToken cancellationToken = default)
            => throw new HandlerNotFoundException(request.GetType());

        private static async ValueTask<TResponse> Adapt<TActual, TResponse>(ValueTask<TActual> source)
        {
            TActual result = await source.ConfigureAwait(false);
            return (TResponse)(object)result!;
        }
    }

    [Fact]
    public void AddPlaxionMediatorCore_Registers_Options()
    {
        ServiceCollection services = new();
        services.AddPlaxionMediatorCore(o => o.DefaultHandlerLifetime = ServiceLifetime.Singleton);

        using ServiceProvider sp = services.BuildServiceProvider();
        PlaxionMediatorOptions options = sp.GetRequiredService<PlaxionMediatorOptions>();
        Assert.Equal(ServiceLifetime.Singleton, options.DefaultHandlerLifetime);
    }

    [Fact]
    public async Task Behaviors_Are_Resolved_And_Executed_In_Registration_Order()
    {
        List<string> log = [];
        ServiceCollection services = new();
        services.AddPlaxionMediatorCore();
        services.AddSingleton(log);
        services.AddScoped<IRequestHandler<Ping, string>, PingHandler>();
        
        // Behaviors registered in order
        services.AddScoped<IPipelineBehavior<Ping, string>, BehaviorA>();
        services.AddScoped<IPipelineBehavior<Ping, string>, BehaviorB>();
        
        services.AddPlaxionMediatorDispatcher<TestDispatcher>();

        await using ServiceProvider sp = services.BuildServiceProvider();
        ISender sender = sp.GetRequiredService<ISender>();

        await sender.Send(new Ping("test"));
        
        Assert.Equal(["A-Start", "B-Start", "Handler", "B-End", "A-End"], log);
    }

    private sealed class BehaviorA : IPipelineBehavior<Ping, string>
    {
        private readonly List<string> _log;
        public BehaviorA(List<string> log) => _log = log;
        public async ValueTask<string> Handle(Ping request, RequestHandlerDelegate<string> next, CancellationToken ct)
        {
            _log.Add("A-Start");
            var res = await next();
            _log.Add("A-End");
            return res;
        }
    }

    private sealed class BehaviorB : IPipelineBehavior<Ping, string>
    {
        private readonly List<string> _log;
        public BehaviorB(List<string> log) => _log = log;
        public async ValueTask<string> Handle(Ping request, RequestHandlerDelegate<string> next, CancellationToken ct)
        {
            _log.Add("B-Start");
            var res = await next();
            _log.Add("B-End");
            return res;
        }
    }

    [Fact]
    public async Task AddPlaxionMediator_With_Manual_Dispatcher_And_Handler_Works()
    {
        ServiceCollection services = new();
        services.AddPlaxionMediatorCore();
        services.AddScoped<IRequestHandler<Ping, string>, PingHandler>();
        services.AddPlaxionMediatorDispatcher<TestDispatcher>();

        await using ServiceProvider sp = services.BuildServiceProvider();
        using IServiceScope scope = sp.CreateScope();
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();

        string result = await sender.Send(new Ping("hi"));
        Assert.Equal("Pong:hi", result);
    }

    [Fact]
    public void AddPlaxionMediator_Invokes_Generated_Bridge_When_Set()
    {
        bool called = false;
        PlaxionMediatorGeneratedRegistrationBridge.Register = (services, options) =>
        {
            called = true;
            services.AddSingleton(new object());
        };

        try
        {
            ServiceCollection services = new();
            services.AddPlaxionMediator();
            Assert.True(called);
            Assert.Contains(services, d => d.ServiceType == typeof(object));
        }
        finally
        {
            PlaxionMediatorGeneratedRegistrationBridge.Register = null;
        }
    }

    [Fact]
    public void PlaxionMediatorOptions_GlobalBehaviors_Is_Mutable_List()
    {
        PlaxionMediatorOptions options = new();
        options.GlobalBehaviors.Add(typeof(string));
        Assert.Single(options.GlobalBehaviors);
    }
}

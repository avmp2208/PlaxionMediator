using Comparison.Shared;
using Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace Comparison.MediatorAdapter;

public sealed record MediatorPipelineRequest(ScenarioPayload Payload) : IRequest<string>;
public sealed class MediatorPipelineHandler : IRequestHandler<MediatorPipelineRequest, string>
{
    public ValueTask<string> Handle(MediatorPipelineRequest request, CancellationToken cancellationToken) => new(request.Payload.Data);
}

public sealed class MediatorBehavior01<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ISimulatedWork _work = new SimulatedValidationWork();
    public async ValueTask<TResponse> Handle(TRequest message, MessageHandlerDelegate<TRequest, TResponse> next, CancellationToken cancellationToken)
    {
        _work.Do(new ScenarioPayload("behavior01", "data"));
        return await next(message, cancellationToken);
    }
}

public sealed class MediatorBehavior02<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ISimulatedWork _work = new SimulatedValidationWork();
    public async ValueTask<TResponse> Handle(TRequest message, MessageHandlerDelegate<TRequest, TResponse> next, CancellationToken cancellationToken)
    {
        _work.Do(new ScenarioPayload("behavior02", "data"));
        return await next(message, cancellationToken);
    }
}

public sealed class MediatorBehavior03<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ISimulatedWork _work = new SimulatedValidationWork();
    public async ValueTask<TResponse> Handle(TRequest message, MessageHandlerDelegate<TRequest, TResponse> next, CancellationToken cancellationToken)
    {
        _work.Do(new ScenarioPayload("behavior03", "data"));
        return await next(message, cancellationToken);
    }
}

public sealed class MediatorBehavior04<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ISimulatedWork _work = new SimulatedValidationWork();
    public async ValueTask<TResponse> Handle(TRequest message, MessageHandlerDelegate<TRequest, TResponse> next, CancellationToken cancellationToken)
    {
        _work.Do(new ScenarioPayload("behavior04", "data"));
        return await next(message, cancellationToken);
    }
}

public sealed class MediatorBehavior05<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ISimulatedWork _work = new SimulatedValidationWork();
    public async ValueTask<TResponse> Handle(TRequest message, MessageHandlerDelegate<TRequest, TResponse> next, CancellationToken cancellationToken)
    {
        _work.Do(new ScenarioPayload("behavior05", "data"));
        return await next(message, cancellationToken);
    }
}

public sealed class MediatorBehavior06<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ISimulatedWork _work = new SimulatedValidationWork();
    public async ValueTask<TResponse> Handle(TRequest message, MessageHandlerDelegate<TRequest, TResponse> next, CancellationToken cancellationToken)
    {
        _work.Do(new ScenarioPayload("behavior06", "data"));
        return await next(message, cancellationToken);
    }
}

public sealed class MediatorBehavior07<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ISimulatedWork _work = new SimulatedValidationWork();
    public async ValueTask<TResponse> Handle(TRequest message, MessageHandlerDelegate<TRequest, TResponse> next, CancellationToken cancellationToken)
    {
        _work.Do(new ScenarioPayload("behavior07", "data"));
        return await next(message, cancellationToken);
    }
}

public sealed class MediatorBehavior08<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ISimulatedWork _work = new SimulatedValidationWork();
    public async ValueTask<TResponse> Handle(TRequest message, MessageHandlerDelegate<TRequest, TResponse> next, CancellationToken cancellationToken)
    {
        _work.Do(new ScenarioPayload("behavior08", "data"));
        return await next(message, cancellationToken);
    }
}

public sealed class MediatorBehavior09<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ISimulatedWork _work = new SimulatedValidationWork();
    public async ValueTask<TResponse> Handle(TRequest message, MessageHandlerDelegate<TRequest, TResponse> next, CancellationToken cancellationToken)
    {
        _work.Do(new ScenarioPayload("behavior09", "data"));
        return await next(message, cancellationToken);
    }
}

public sealed class MediatorBehavior10<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ISimulatedWork _work = new SimulatedValidationWork();
    public async ValueTask<TResponse> Handle(TRequest message, MessageHandlerDelegate<TRequest, TResponse> next, CancellationToken cancellationToken)
    {
        _work.Do(new ScenarioPayload("behavior10", "data"));
        return await next(message, cancellationToken);
    }
}

public sealed class MediatorBehavior11<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ISimulatedWork _work = new SimulatedValidationWork();
    public async ValueTask<TResponse> Handle(TRequest message, MessageHandlerDelegate<TRequest, TResponse> next, CancellationToken cancellationToken)
    {
        _work.Do(new ScenarioPayload("behavior11", "data"));
        return await next(message, cancellationToken);
    }
}

public sealed class MediatorBehavior12<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ISimulatedWork _work = new SimulatedValidationWork();
    public async ValueTask<TResponse> Handle(TRequest message, MessageHandlerDelegate<TRequest, TResponse> next, CancellationToken cancellationToken)
    {
        _work.Do(new ScenarioPayload("behavior12", "data"));
        return await next(message, cancellationToken);
    }
}

public sealed class MediatorBehavior13<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ISimulatedWork _work = new SimulatedValidationWork();
    public async ValueTask<TResponse> Handle(TRequest message, MessageHandlerDelegate<TRequest, TResponse> next, CancellationToken cancellationToken)
    {
        _work.Do(new ScenarioPayload("behavior13", "data"));
        return await next(message, cancellationToken);
    }
}

public sealed class MediatorBehavior14<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ISimulatedWork _work = new SimulatedValidationWork();
    public async ValueTask<TResponse> Handle(TRequest message, MessageHandlerDelegate<TRequest, TResponse> next, CancellationToken cancellationToken)
    {
        _work.Do(new ScenarioPayload("behavior14", "data"));
        return await next(message, cancellationToken);
    }
}

public sealed class MediatorBehavior15<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ISimulatedWork _work = new SimulatedValidationWork();
    public async ValueTask<TResponse> Handle(TRequest message, MessageHandlerDelegate<TRequest, TResponse> next, CancellationToken cancellationToken)
    {
        _work.Do(new ScenarioPayload("behavior15", "data"));
        return await next(message, cancellationToken);
    }
}

public sealed class MediatorBehavior16<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ISimulatedWork _work = new SimulatedValidationWork();
    public async ValueTask<TResponse> Handle(TRequest message, MessageHandlerDelegate<TRequest, TResponse> next, CancellationToken cancellationToken)
    {
        _work.Do(new ScenarioPayload("behavior16", "data"));
        return await next(message, cancellationToken);
    }
}

public sealed class MediatorBehavior17<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ISimulatedWork _work = new SimulatedValidationWork();
    public async ValueTask<TResponse> Handle(TRequest message, MessageHandlerDelegate<TRequest, TResponse> next, CancellationToken cancellationToken)
    {
        _work.Do(new ScenarioPayload("behavior17", "data"));
        return await next(message, cancellationToken);
    }
}

public sealed class MediatorBehavior18<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ISimulatedWork _work = new SimulatedValidationWork();
    public async ValueTask<TResponse> Handle(TRequest message, MessageHandlerDelegate<TRequest, TResponse> next, CancellationToken cancellationToken)
    {
        _work.Do(new ScenarioPayload("behavior18", "data"));
        return await next(message, cancellationToken);
    }
}

public sealed class MediatorBehavior19<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ISimulatedWork _work = new SimulatedValidationWork();
    public async ValueTask<TResponse> Handle(TRequest message, MessageHandlerDelegate<TRequest, TResponse> next, CancellationToken cancellationToken)
    {
        _work.Do(new ScenarioPayload("behavior19", "data"));
        return await next(message, cancellationToken);
    }
}

public sealed class MediatorBehavior20<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ISimulatedWork _work = new SimulatedValidationWork();
    public async ValueTask<TResponse> Handle(TRequest message, MessageHandlerDelegate<TRequest, TResponse> next, CancellationToken cancellationToken)
    {
        _work.Do(new ScenarioPayload("behavior20", "data"));
        return await next(message, cancellationToken);
    }
}

public static class MediatorPipelineRegistrar
{
    private static readonly Type[] BehaviorTypes = new[]
    {
        typeof(MediatorBehavior01<,>),
        typeof(MediatorBehavior02<,>),
        typeof(MediatorBehavior03<,>),
        typeof(MediatorBehavior04<,>),
        typeof(MediatorBehavior05<,>),
        typeof(MediatorBehavior06<,>),
        typeof(MediatorBehavior07<,>),
        typeof(MediatorBehavior08<,>),
        typeof(MediatorBehavior09<,>),
        typeof(MediatorBehavior10<,>),
        typeof(MediatorBehavior11<,>),
        typeof(MediatorBehavior12<,>),
        typeof(MediatorBehavior13<,>),
        typeof(MediatorBehavior14<,>),
        typeof(MediatorBehavior15<,>),
        typeof(MediatorBehavior16<,>),
        typeof(MediatorBehavior17<,>),
        typeof(MediatorBehavior18<,>),
        typeof(MediatorBehavior19<,>),
        typeof(MediatorBehavior20<,>),
    };

    public static void RegisterBehaviors(IServiceCollection services, int count)
    {
        for (int i = 0; i < count; i++)
        {
            services.AddSingleton(typeof(IPipelineBehavior<,>), BehaviorTypes[i]);
        }
    }
}


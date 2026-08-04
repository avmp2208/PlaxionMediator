using Comparison.Shared;
using PlaxionMediator.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Comparison.PlaxionAdapter;

public record PlaxionPipelineRequest(ScenarioPayload Payload) : IRequest<string>;
public class PlaxionPipelineHandler : IRequestHandler<PlaxionPipelineRequest, string>
{
    public ValueTask<string> Handle(PlaxionPipelineRequest request, CancellationToken cancellationToken) => new(request.Payload.Data);
}

public class PlaxionBehavior01<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ISimulatedWork _work = new SimulatedValidationWork();
    public async ValueTask<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _work.Do(new ScenarioPayload("behavior01", "data"));
        return await next();
    }
}

public class PlaxionBehavior02<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ISimulatedWork _work = new SimulatedValidationWork();
    public async ValueTask<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _work.Do(new ScenarioPayload("behavior02", "data"));
        return await next();
    }
}

public class PlaxionBehavior03<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ISimulatedWork _work = new SimulatedValidationWork();
    public async ValueTask<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _work.Do(new ScenarioPayload("behavior03", "data"));
        return await next();
    }
}

public class PlaxionBehavior04<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ISimulatedWork _work = new SimulatedValidationWork();
    public async ValueTask<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _work.Do(new ScenarioPayload("behavior04", "data"));
        return await next();
    }
}

public class PlaxionBehavior05<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ISimulatedWork _work = new SimulatedValidationWork();
    public async ValueTask<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _work.Do(new ScenarioPayload("behavior05", "data"));
        return await next();
    }
}

public class PlaxionBehavior06<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ISimulatedWork _work = new SimulatedValidationWork();
    public async ValueTask<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _work.Do(new ScenarioPayload("behavior06", "data"));
        return await next();
    }
}

public class PlaxionBehavior07<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ISimulatedWork _work = new SimulatedValidationWork();
    public async ValueTask<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _work.Do(new ScenarioPayload("behavior07", "data"));
        return await next();
    }
}

public class PlaxionBehavior08<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ISimulatedWork _work = new SimulatedValidationWork();
    public async ValueTask<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _work.Do(new ScenarioPayload("behavior08", "data"));
        return await next();
    }
}

public class PlaxionBehavior09<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ISimulatedWork _work = new SimulatedValidationWork();
    public async ValueTask<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _work.Do(new ScenarioPayload("behavior09", "data"));
        return await next();
    }
}

public class PlaxionBehavior10<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ISimulatedWork _work = new SimulatedValidationWork();
    public async ValueTask<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _work.Do(new ScenarioPayload("behavior10", "data"));
        return await next();
    }
}

public class PlaxionBehavior11<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ISimulatedWork _work = new SimulatedValidationWork();
    public async ValueTask<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _work.Do(new ScenarioPayload("behavior11", "data"));
        return await next();
    }
}

public class PlaxionBehavior12<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ISimulatedWork _work = new SimulatedValidationWork();
    public async ValueTask<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _work.Do(new ScenarioPayload("behavior12", "data"));
        return await next();
    }
}

public class PlaxionBehavior13<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ISimulatedWork _work = new SimulatedValidationWork();
    public async ValueTask<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _work.Do(new ScenarioPayload("behavior13", "data"));
        return await next();
    }
}

public class PlaxionBehavior14<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ISimulatedWork _work = new SimulatedValidationWork();
    public async ValueTask<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _work.Do(new ScenarioPayload("behavior14", "data"));
        return await next();
    }
}

public class PlaxionBehavior15<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ISimulatedWork _work = new SimulatedValidationWork();
    public async ValueTask<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _work.Do(new ScenarioPayload("behavior15", "data"));
        return await next();
    }
}

public class PlaxionBehavior16<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ISimulatedWork _work = new SimulatedValidationWork();
    public async ValueTask<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _work.Do(new ScenarioPayload("behavior16", "data"));
        return await next();
    }
}

public class PlaxionBehavior17<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ISimulatedWork _work = new SimulatedValidationWork();
    public async ValueTask<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _work.Do(new ScenarioPayload("behavior17", "data"));
        return await next();
    }
}

public class PlaxionBehavior18<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ISimulatedWork _work = new SimulatedValidationWork();
    public async ValueTask<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _work.Do(new ScenarioPayload("behavior18", "data"));
        return await next();
    }
}

public class PlaxionBehavior19<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ISimulatedWork _work = new SimulatedValidationWork();
    public async ValueTask<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _work.Do(new ScenarioPayload("behavior19", "data"));
        return await next();
    }
}

public class PlaxionBehavior20<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ISimulatedWork _work = new SimulatedValidationWork();
    public async ValueTask<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _work.Do(new ScenarioPayload("behavior20", "data"));
        return await next();
    }
}

public static class PlaxionPipelineRegistrar
{
    private static readonly Type[] BehaviorTypes = new[]
    {
        typeof(PlaxionBehavior01<,>),
        typeof(PlaxionBehavior02<,>),
        typeof(PlaxionBehavior03<,>),
        typeof(PlaxionBehavior04<,>),
        typeof(PlaxionBehavior05<,>),
        typeof(PlaxionBehavior06<,>),
        typeof(PlaxionBehavior07<,>),
        typeof(PlaxionBehavior08<,>),
        typeof(PlaxionBehavior09<,>),
        typeof(PlaxionBehavior10<,>),
        typeof(PlaxionBehavior11<,>),
        typeof(PlaxionBehavior12<,>),
        typeof(PlaxionBehavior13<,>),
        typeof(PlaxionBehavior14<,>),
        typeof(PlaxionBehavior15<,>),
        typeof(PlaxionBehavior16<,>),
        typeof(PlaxionBehavior17<,>),
        typeof(PlaxionBehavior18<,>),
        typeof(PlaxionBehavior19<,>),
        typeof(PlaxionBehavior20<,>),
    };

    public static void RegisterBehaviors(IServiceCollection services, int count)
    {
        for (int i = 0; i < count; i++)
        {
            services.AddSingleton(typeof(IPipelineBehavior<,>), BehaviorTypes[i]);
        }
    }
}


using Comparison.Shared;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Comparison.MediatRAdapter;

public record MediatRPipelineRequest(ScenarioPayload Payload) : IRequest<string>;
public class MediatRPipelineHandler : IRequestHandler<MediatRPipelineRequest, string>
{
    public Task<string> Handle(MediatRPipelineRequest request, CancellationToken cancellationToken) => Task.FromResult(request.Payload.Data);
}

public class MediatRBehavior01<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ISimulatedWork _work = new SimulatedValidationWork();
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _work.Do(new ScenarioPayload("behavior01", "data"));
        return await next();
    }
}

public class MediatRBehavior02<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ISimulatedWork _work = new SimulatedValidationWork();
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _work.Do(new ScenarioPayload("behavior02", "data"));
        return await next();
    }
}

public class MediatRBehavior03<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ISimulatedWork _work = new SimulatedValidationWork();
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _work.Do(new ScenarioPayload("behavior03", "data"));
        return await next();
    }
}

public class MediatRBehavior04<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ISimulatedWork _work = new SimulatedValidationWork();
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _work.Do(new ScenarioPayload("behavior04", "data"));
        return await next();
    }
}

public class MediatRBehavior05<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ISimulatedWork _work = new SimulatedValidationWork();
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _work.Do(new ScenarioPayload("behavior05", "data"));
        return await next();
    }
}

public class MediatRBehavior06<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ISimulatedWork _work = new SimulatedValidationWork();
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _work.Do(new ScenarioPayload("behavior06", "data"));
        return await next();
    }
}

public class MediatRBehavior07<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ISimulatedWork _work = new SimulatedValidationWork();
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _work.Do(new ScenarioPayload("behavior07", "data"));
        return await next();
    }
}

public class MediatRBehavior08<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ISimulatedWork _work = new SimulatedValidationWork();
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _work.Do(new ScenarioPayload("behavior08", "data"));
        return await next();
    }
}

public class MediatRBehavior09<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ISimulatedWork _work = new SimulatedValidationWork();
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _work.Do(new ScenarioPayload("behavior09", "data"));
        return await next();
    }
}

public class MediatRBehavior10<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ISimulatedWork _work = new SimulatedValidationWork();
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _work.Do(new ScenarioPayload("behavior10", "data"));
        return await next();
    }
}

public class MediatRBehavior11<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ISimulatedWork _work = new SimulatedValidationWork();
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _work.Do(new ScenarioPayload("behavior11", "data"));
        return await next();
    }
}

public class MediatRBehavior12<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ISimulatedWork _work = new SimulatedValidationWork();
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _work.Do(new ScenarioPayload("behavior12", "data"));
        return await next();
    }
}

public class MediatRBehavior13<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ISimulatedWork _work = new SimulatedValidationWork();
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _work.Do(new ScenarioPayload("behavior13", "data"));
        return await next();
    }
}

public class MediatRBehavior14<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ISimulatedWork _work = new SimulatedValidationWork();
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _work.Do(new ScenarioPayload("behavior14", "data"));
        return await next();
    }
}

public class MediatRBehavior15<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ISimulatedWork _work = new SimulatedValidationWork();
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _work.Do(new ScenarioPayload("behavior15", "data"));
        return await next();
    }
}

public class MediatRBehavior16<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ISimulatedWork _work = new SimulatedValidationWork();
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _work.Do(new ScenarioPayload("behavior16", "data"));
        return await next();
    }
}

public class MediatRBehavior17<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ISimulatedWork _work = new SimulatedValidationWork();
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _work.Do(new ScenarioPayload("behavior17", "data"));
        return await next();
    }
}

public class MediatRBehavior18<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ISimulatedWork _work = new SimulatedValidationWork();
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _work.Do(new ScenarioPayload("behavior18", "data"));
        return await next();
    }
}

public class MediatRBehavior19<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ISimulatedWork _work = new SimulatedValidationWork();
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _work.Do(new ScenarioPayload("behavior19", "data"));
        return await next();
    }
}

public class MediatRBehavior20<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ISimulatedWork _work = new SimulatedValidationWork();
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _work.Do(new ScenarioPayload("behavior20", "data"));
        return await next();
    }
}

public static class MediatRPipelineRegistrar
{
    private static readonly Type[] BehaviorTypes = new[]
    {
        typeof(MediatRBehavior01<,>),
        typeof(MediatRBehavior02<,>),
        typeof(MediatRBehavior03<,>),
        typeof(MediatRBehavior04<,>),
        typeof(MediatRBehavior05<,>),
        typeof(MediatRBehavior06<,>),
        typeof(MediatRBehavior07<,>),
        typeof(MediatRBehavior08<,>),
        typeof(MediatRBehavior09<,>),
        typeof(MediatRBehavior10<,>),
        typeof(MediatRBehavior11<,>),
        typeof(MediatRBehavior12<,>),
        typeof(MediatRBehavior13<,>),
        typeof(MediatRBehavior14<,>),
        typeof(MediatRBehavior15<,>),
        typeof(MediatRBehavior16<,>),
        typeof(MediatRBehavior17<,>),
        typeof(MediatRBehavior18<,>),
        typeof(MediatRBehavior19<,>),
        typeof(MediatRBehavior20<,>),
    };

    public static void RegisterBehaviors(IServiceCollection services, int count)
    {
        for (int i = 0; i < count; i++)
        {
            services.AddSingleton(typeof(IPipelineBehavior<,>), BehaviorTypes[i]);
        }
    }
}


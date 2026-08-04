using Microsoft.Extensions.DependencyInjection;

namespace Comparison.MediatRAdapter;

public static class MediatRAdapterFactory
{
    public static ServiceProvider BuildServiceProviderForBehaviors(int behaviorCount)
    {
        var services = new ServiceCollection();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<MediatRPipelineHandler>());
        MediatRPipelineRegistrar.RegisterBehaviors(services, behaviorCount);
        return services.BuildServiceProvider();
    }

    public static ServiceProvider BuildServiceProviderForTypeVariety()
    {
        var services = new ServiceCollection();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<MediatRPipelineHandler>());
        return services.BuildServiceProvider();
    }

    public static ServiceProvider BuildServiceProviderForNotifications(int handlerCount)
    {
        var services = new ServiceCollection();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<MediatRPipelineHandler>());
        MediatRNotificationRegistrar.RegisterHandlers(services, handlerCount);
        return services.BuildServiceProvider();
    }

    public static ServiceProvider BuildServiceProviderForConcurrency()
    {
        var services = new ServiceCollection();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<MediatRPipelineHandler>());
        // For concurrency benchmarks, we just need the dispatcher and the pipeline request handler.
        // No extra behaviors.
        return services.BuildServiceProvider();
    }
}
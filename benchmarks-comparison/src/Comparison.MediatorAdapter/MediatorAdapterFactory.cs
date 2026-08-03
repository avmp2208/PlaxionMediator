using Microsoft.Extensions.DependencyInjection;

namespace Comparison.MediatorAdapter;

public static class MediatorAdapterFactory
{
    public static ServiceProvider BuildServiceProviderForBehaviors(int behaviorCount)
    {
        var services = new ServiceCollection();
        services.AddMediator();
        MediatorPipelineRegistrar.RegisterBehaviors(services, behaviorCount);
        return services.BuildServiceProvider();
    }

    public static ServiceProvider BuildServiceProviderForTypeVariety()
    {
        var services = new ServiceCollection();
        services.AddMediator();
        return services.BuildServiceProvider();
    }

    public static ServiceProvider BuildServiceProviderForNotifications(int handlerCount)
    {
        var services = new ServiceCollection();
        services.AddMediator();
        MediatorNotificationRegistrar.RegisterHandlers(services, handlerCount);
        return services.BuildServiceProvider();
    }

    public static ServiceProvider BuildServiceProviderForConcurrency()
    {
        var services = new ServiceCollection();
        services.AddMediator();
        // For concurrency benchmarks, we just need the dispatcher and the pipeline request handler.
        // No extra behaviors.
        return services.BuildServiceProvider();
    }
}
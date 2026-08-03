using Microsoft.Extensions.DependencyInjection;
using PlaxionMediator;

namespace Comparison.PlaxionAdapter;

public static class PlaxionAdapterFactory
{
    public static ServiceProvider BuildServiceProviderForBehaviors(int behaviorCount)
    {
        var services = new ServiceCollection();
        // Option 1: Add to options.GlobalBehaviors and then call AddPlaxionMediator
        services.AddPlaxionMediator(options =>
        {
            // We could add them here, but the registrar does it via IServiceCollection.
        });
        
        // Option 2: Use the registrar to add directly to services.
        PlaxionPipelineRegistrar.RegisterBehaviors(services, behaviorCount);
        
        return services.BuildServiceProvider();
    }

    public static ServiceProvider BuildServiceProviderForTypeVariety()
    {
        var services = new ServiceCollection();
        // AddPlaxionMediator discovers them automatically via source-gen bridge.
        services.AddPlaxionMediator();
        return services.BuildServiceProvider();
    }

    public static ServiceProvider BuildServiceProviderForNotifications(int handlerCount)
    {
        var services = new ServiceCollection();
        services.AddPlaxionMediator();
        PlaxionNotificationRegistrar.RegisterHandlers(services, handlerCount);
        return services.BuildServiceProvider();
    }

    public static ServiceProvider BuildServiceProviderForConcurrency()
    {
        var services = new ServiceCollection();
        services.AddPlaxionMediator();
        // For concurrency benchmarks, we just need the dispatcher and the pipeline request handler.
        // No extra behaviors.
        return services.BuildServiceProvider();
    }
}

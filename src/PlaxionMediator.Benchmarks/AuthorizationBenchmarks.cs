using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Microsoft.Extensions.DependencyInjection;
using PlaxionMediator.Abstractions;
using PlaxionMediator.Core;
using PlaxionMediator.Authorization;

namespace PlaxionMediator.Benchmarks;

/// <summary>
/// Authorization pipeline overhead benchmarks.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class AuthorizationBenchmarks
{
    private ServiceProvider _baselineProvider = null!;
    private ServiceProvider _fastPathProvider = null!;
    private ServiceProvider _oneCheckProvider = null!;
    private ServiceProvider _threeChecksProvider = null!;
    private ServiceProvider _deniedProvider = null!;
    
    private ISender _baselineSender = null!;
    private ISender _fastPathSender = null!;
    private ISender _oneCheckSender = null!;
    private ISender _threeChecksSender = null!;
    private ISender _deniedSender = null!;

    [GlobalSetup]
    public void Setup()
    {
        // 1) Baseline: No authorization behavior registered at all
        var baseline = new ServiceCollection();
        baseline.AddPlaxionMediator();
        _baselineProvider = baseline.BuildServiceProvider();
        _baselineSender = _baselineProvider.GetRequiredService<ISender>();

        // 2) AuthorizationBehavior registered, but no IRequestAuthorization checks for AuthPing (fast no-op path)
        var fastPath = new ServiceCollection();
        fastPath.AddPlaxionMediator(o => o.UsePlaxionMediatorAuthorizationBehavior());
        fastPath.AddPlaxionMediatorAuthorization();
        _fastPathProvider = fastPath.BuildServiceProvider();
        _fastPathSender = _fastPathProvider.GetRequiredService<ISender>();

        // 3) One successful check
        var oneCheck = new ServiceCollection();
        oneCheck.AddPlaxionMediator(o => o.UsePlaxionMediatorAuthorizationBehavior());
        oneCheck.AddPlaxionMediatorAuthorization();
        oneCheck.AddSingleton<IRequestAuthorization<AuthPingOneCheck>, SuccessfulCheckOne>();
        _oneCheckProvider = oneCheck.BuildServiceProvider();
        _oneCheckSender = _oneCheckProvider.GetRequiredService<ISender>();

        // 4) Multiple (3) successful checks
        var threeChecks = new ServiceCollection();
        threeChecks.AddPlaxionMediator(o => o.UsePlaxionMediatorAuthorizationBehavior());
        threeChecks.AddPlaxionMediatorAuthorization();
        threeChecks.AddSingleton<IRequestAuthorization<AuthPingThreeChecks>, SuccessfulCheckThree>();
        threeChecks.AddSingleton<IRequestAuthorization<AuthPingThreeChecks>, SuccessfulCheckThree>();
        threeChecks.AddSingleton<IRequestAuthorization<AuthPingThreeChecks>, SuccessfulCheckThree>();
        _threeChecksProvider = threeChecks.BuildServiceProvider();
        _threeChecksSender = _threeChecksProvider.GetRequiredService<ISender>();

        // 5) Denied path (one check returns Forbidden, exception thrown and caught)
        var denied = new ServiceCollection();
        denied.AddPlaxionMediator(o => o.UsePlaxionMediatorAuthorizationBehavior());
        denied.AddPlaxionMediatorAuthorization();
        denied.AddSingleton<IRequestAuthorization<AuthPingDenied>, ForbiddenCheckDenied>();
        _deniedProvider = denied.BuildServiceProvider();
        _deniedSender = _deniedProvider.GetRequiredService<ISender>();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _baselineProvider.Dispose();
        _fastPathProvider.Dispose();
        _oneCheckProvider.Dispose();
        _threeChecksProvider.Dispose();
        _deniedProvider.Dispose();
    }

    [Benchmark(Baseline = true, Description = "Send_Baseline_NoAuth")]
    public ValueTask<string> Send_Baseline_NoAuth() 
        => _baselineSender.Send(new AuthPing("benchmark"));

    [Benchmark(Description = "Send_AuthBehavior_NoChecks_FastPath")]
    public ValueTask<string> Send_AuthBehavior_NoChecks_FastPath() 
        => _fastPathSender.Send(new AuthPing("benchmark"));

    [Benchmark(Description = "Send_OneCheck_Success")]
    public ValueTask<string> Send_OneCheck_Success() 
        => _oneCheckSender.Send(new AuthPingOneCheck("benchmark"));

    [Benchmark(Description = "Send_ThreeChecks_Success")]
    public ValueTask<string> Send_ThreeChecks_Success() 
        => _threeChecksSender.Send(new AuthPingThreeChecks("benchmark"));

    [Benchmark(Description = "Send_OneCheck_Denied_Exception")]
    public async Task Send_OneCheck_Denied_Exception()
    {
        try
        {
            await _deniedSender.Send(new AuthPingDenied("benchmark"));
        }
        catch (PlaxionMediatorForbiddenException)
        {
            // Expected
        }
    }
}

public sealed record AuthPing(string Message) : IRequest<string>;
public sealed record AuthPingOneCheck(string Message) : IRequest<string>;
public sealed record AuthPingThreeChecks(string Message) : IRequest<string>;
public sealed record AuthPingDenied(string Message) : IRequest<string>;

public sealed class AuthPingHandler : IRequestHandler<AuthPing, string>
{
    public ValueTask<string> Handle(AuthPing request, CancellationToken cancellationToken) => ValueTask.FromResult("Pong");
}

public sealed class AuthPingOneCheckHandler : IRequestHandler<AuthPingOneCheck, string>
{
    public ValueTask<string> Handle(AuthPingOneCheck request, CancellationToken cancellationToken) => ValueTask.FromResult("Pong");
}

public sealed class AuthPingThreeChecksHandler : IRequestHandler<AuthPingThreeChecks, string>
{
    public ValueTask<string> Handle(AuthPingThreeChecks request, CancellationToken cancellationToken) => ValueTask.FromResult("Pong");
}

public sealed class AuthPingDeniedHandler : IRequestHandler<AuthPingDenied, string>
{
    public ValueTask<string> Handle(AuthPingDenied request, CancellationToken cancellationToken) => ValueTask.FromResult("Pong");
}

public sealed class SuccessfulCheckOne : IRequestAuthorization<AuthPingOneCheck>
{
    public ValueTask<AuthorizationOutcome> AuthorizeAsync(AuthPingOneCheck request, IAuthorizationContext context, CancellationToken cancellationToken)
        => ValueTask.FromResult(AuthorizationOutcome.Authorized);
}

public sealed class SuccessfulCheckThree : IRequestAuthorization<AuthPingThreeChecks>
{
    public ValueTask<AuthorizationOutcome> AuthorizeAsync(AuthPingThreeChecks request, IAuthorizationContext context, CancellationToken cancellationToken)
        => ValueTask.FromResult(AuthorizationOutcome.Authorized);
}

public sealed class ForbiddenCheckDenied : IRequestAuthorization<AuthPingDenied>
{
    public ValueTask<AuthorizationOutcome> AuthorizeAsync(AuthPingDenied request, IAuthorizationContext context, CancellationToken cancellationToken)
        => ValueTask.FromResult(AuthorizationOutcome.Forbidden);
}

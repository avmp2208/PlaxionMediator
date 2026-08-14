using Microsoft.Extensions.DependencyInjection;
using PlaxionMediator.Abstractions;
using PlaxionMediator.Authorization;
using PlaxionMediator.Caching;
using PlaxionMediator.Core;
using PlaxionMediator.Retry;
using PlaxionMediator.Transactions;
using PlaxionMediator.Validation;

namespace PlaxionMediator.Tests;

/// <summary>
/// Behavioral-compatibility regression tests covering the documented composed pipeline order
/// (Validation → Authorization → Caching → Retry → Transaction → Handler) per
/// docs/wiki/Authorization.md, docs/wiki/Transactions.md, and docs/wiki/Full-Usage-Guide.md.
/// </summary>
public sealed class ComposedPipelineOrderingTests
{
    private sealed record ComposedRequest(string Key, bool Authorized, bool FailValidation)
        : IRequest<string>, ICacheableRequest<string>, ITransactionalRequest
    {
        public string CacheKey => Key;
    }

    private sealed class ComposedValidator : IPlaxionMediatorValidator<ComposedRequest>
    {
        public ValueTask<PlaxionMediatorValidationResult> ValidateAsync(ComposedRequest request, CancellationToken cancellationToken)
            => ValueTask.FromResult(
                request.FailValidation
                    ? PlaxionMediatorValidationResult.Failed(new PlaxionMediatorValidationFailure(nameof(ComposedRequest.FailValidation), "invalid"))
                    : PlaxionMediatorValidationResult.Success);
    }

    private sealed class ComposedAuthorization : IRequestAuthorization<ComposedRequest>
    {
        public ValueTask<AuthorizationOutcome> AuthorizeAsync(ComposedRequest request, IAuthorizationContext context, CancellationToken cancellationToken)
            => ValueTask.FromResult(request.Authorized ? AuthorizationOutcome.Authorized : AuthorizationOutcome.Forbidden);
    }

    private sealed class RecordingTransactionManager : ITransactionManager
    {
        public int BeginCount;
        public int CommitCount;
        public int RollbackCount;

        public ValueTask<ITransactionScope> BeginAsync(TransactionIsolationLevel isolationLevel, CancellationToken cancellationToken = default)
        {
            BeginCount++;
            return ValueTask.FromResult<ITransactionScope>(new RecordingScope(this));
        }

        private sealed class RecordingScope : ITransactionScope
        {
            private readonly RecordingTransactionManager _owner;
            public RecordingScope(RecordingTransactionManager owner) => _owner = owner;
            public bool HasExistingTransaction => false;
            public ValueTask CommitAsync(CancellationToken cancellationToken = default)
            {
                _owner.CommitCount++;
                return default;
            }
            public ValueTask RollbackAsync(CancellationToken cancellationToken = default)
            {
                _owner.RollbackCount++;
                return default;
            }
            public ValueTask DisposeAsync() => default;
        }
    }

    private sealed class CountingHandler : IRequestHandler<ComposedRequest, string>
    {
        public int InvocationCount;
        public bool ThrowOnHandle;

        public ValueTask<string> Handle(ComposedRequest request, CancellationToken cancellationToken)
        {
            InvocationCount++;
            if (ThrowOnHandle)
            {
                throw new InvalidOperationException("handler failure");
            }

            return ValueTask.FromResult("handled:" + request.Key);
        }
    }

    private sealed class TestDispatcher : ISender
    {
        private readonly IServiceProvider _sp;
        public TestDispatcher(IServiceProvider sp) => _sp = sp;

        public ValueTask<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request is ComposedRequest composed && typeof(TResponse) == typeof(string))
            {
                var handler = _sp.GetRequiredService<IRequestHandler<ComposedRequest, string>>();
                var behaviors = _sp.GetServices<IPipelineBehavior<ComposedRequest, string>>().ToList();
                return Adapt<string, TResponse>(
                    PlaxionMediator.Pipeline.PipelineComposer.ExecuteAsync(composed, behaviors, handler.Handle, cancellationToken));
            }

            throw new HandlerNotFoundException(request.GetType());
        }

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
            => throw new HandlerNotFoundException(request.GetType());

        private static async ValueTask<TResponse> Adapt<TActual, TResponse>(ValueTask<TActual> source)
        {
            TActual result = await source.ConfigureAwait(false);
            return (TResponse)(object)result!;
        }
    }

    private static (ServiceProvider Provider, CountingHandler Handler, RecordingTransactionManager TransactionManager) BuildProvider()
    {
        ServiceCollection services = new();
        CountingHandler handler = new();
        RecordingTransactionManager transactionManager = new();

        services.AddPlaxionMediatorCore(o =>
        {
            // Documented recommended composed order (outer -> inner):
            // Validation -> Authorization -> Caching -> Retry -> Transaction -> Handler
            o.UsePlaxionMediatorValidationBehavior();
            o.UsePlaxionMediatorAuthorizationBehavior();
            o.UsePlaxionMediatorCachingBehavior();
            o.UsePlaxionMediatorRetryBehavior();
            o.UsePlaxionMediatorTransactionBehavior();
        });

        services.AddSingleton(handler);
        services.AddScoped<IRequestHandler<ComposedRequest, string>>(sp => sp.GetRequiredService<CountingHandler>());

        services.AddPlaxionMediatorValidator<ComposedRequest, ComposedValidator>();
        services.AddPlaxionMediatorAuthorization();
        services.AddPlaxionMediatorAuthorization<ComposedRequest, ComposedAuthorization>();
        services.AddPlaxionMediatorCaching();
        services.AddPlaxionMediatorRetry(o =>
        {
            o.MaxRetryAttempts = 2;
            o.BaseDelay = TimeSpan.Zero;
        });
        services.AddPlaxionMediatorTransactions();
        services.AddSingleton<ITransactionManager>(transactionManager);

        services.AddScoped<ISender>(sp => new TestDispatcher(sp));

        ServiceProvider provider = services.BuildServiceProvider();
        return (provider, handler, transactionManager);
    }

    [Fact]
    public async Task ComposedPipeline_HappyPath_InvokesHandlerOnceAndCommitsTransaction()
    {
        var (provider, handler, txManager) = BuildProvider();
        await using ServiceProvider sp = provider;
        using IServiceScope scope = sp.CreateScope();
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();

        string result = await sender.Send(new ComposedRequest("happy", Authorized: true, FailValidation: false));

        Assert.Equal("handled:happy", result);
        Assert.Equal(1, handler.InvocationCount);
        Assert.Equal(1, txManager.BeginCount);
        Assert.Equal(1, txManager.CommitCount);
        Assert.Equal(0, txManager.RollbackCount);
    }

    [Fact]
    public async Task ComposedPipeline_ValidationFailure_ShortCircuitsBeforeAuthorizationAndHandler()
    {
        var (provider, handler, txManager) = BuildProvider();
        await using ServiceProvider sp = provider;
        using IServiceScope scope = sp.CreateScope();
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();

        // Unauthorized + invalid: if validation truly runs first, we get a validation exception, not a forbidden one.
        await Assert.ThrowsAsync<PlaxionMediatorValidationException>(
            () => sender.Send(new ComposedRequest("invalid", Authorized: false, FailValidation: true)).AsTask());

        Assert.Equal(0, handler.InvocationCount);
        Assert.Equal(0, txManager.BeginCount);
    }

    [Fact]
    public async Task ComposedPipeline_AuthorizationDenied_NeverReachesHandlerOrTransaction()
    {
        var (provider, handler, txManager) = BuildProvider();
        await using ServiceProvider sp = provider;
        using IServiceScope scope = sp.CreateScope();
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();

        await Assert.ThrowsAsync<PlaxionMediatorForbiddenException>(
            () => sender.Send(new ComposedRequest("denied", Authorized: false, FailValidation: false)).AsTask());

        Assert.Equal(0, handler.InvocationCount);
        Assert.Equal(0, txManager.BeginCount);
    }

    [Fact]
    public async Task ComposedPipeline_CacheHit_ShortCircuitsHandlerAndTransaction()
    {
        var (provider, handler, txManager) = BuildProvider();
        await using ServiceProvider sp = provider;

        ComposedRequest request = new("cache-key", Authorized: true, FailValidation: false);

        using (IServiceScope scope1 = sp.CreateScope())
        {
            ISender sender1 = scope1.ServiceProvider.GetRequiredService<ISender>();
            string first = await sender1.Send(request);
            Assert.Equal("handled:cache-key", first);
        }

        Assert.Equal(1, handler.InvocationCount);
        Assert.Equal(1, txManager.BeginCount);

        using (IServiceScope scope2 = sp.CreateScope())
        {
            ISender sender2 = scope2.ServiceProvider.GetRequiredService<ISender>();
            string second = await sender2.Send(request);
            Assert.Equal("handled:cache-key", second);
        }

        // Cache hit on the second call: handler and transaction must not run again.
        Assert.Equal(1, handler.InvocationCount);
        Assert.Equal(1, txManager.BeginCount);
    }

    [Fact]
    public async Task ComposedPipeline_HandlerFailure_RollsBackTransactionAndRethrows()
    {
        var (provider, handler, txManager) = BuildProvider();
        handler.ThrowOnHandle = true;
        await using ServiceProvider sp = provider;
        using IServiceScope scope = sp.CreateScope();
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sender.Send(new ComposedRequest("boom", Authorized: true, FailValidation: false)).AsTask());

        // Retry (MaxRetryAttempts = 2) means up to 3 attempts, each with its own transaction.
        Assert.True(handler.InvocationCount >= 1);
        Assert.Equal(txManager.BeginCount, txManager.RollbackCount);
        Assert.Equal(0, txManager.CommitCount);
    }
}

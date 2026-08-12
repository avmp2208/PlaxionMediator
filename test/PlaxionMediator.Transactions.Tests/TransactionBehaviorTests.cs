using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using PlaxionMediator.Abstractions;
using PlaxionMediator.Core;
using PlaxionMediator.Retry;
using Xunit;

namespace PlaxionMediator.Transactions.Tests;

public sealed class TransactionBehaviorTests
{
    private sealed record Ping(string Message) : IRequest<string>;

    private sealed record TxPing(string Message) : IRequest<string>, ITransactionalRequest
    {
        public TransactionIsolationLevel? IsolationLevel { get; init; }
    }

    private sealed record RetryableTxPing(string Message) : IRequest<string>, ITransactionalRequest, IRetryableRequest
    {
        public int? MaxRetryAttempts => 2;
        public TimeSpan? BaseDelay => TimeSpan.Zero;
    }

    private sealed class RecordingScope : ITransactionScope
    {
        private readonly RecordingTransactionManager _owner;
        private readonly bool _ownsTransaction;
        private int _commitCount;
        private int _rollbackCount;

        public RecordingScope(RecordingTransactionManager owner, bool ownsTransaction)
        {
            _owner = owner;
            _ownsTransaction = ownsTransaction;
            owner.ActiveScopes++;
        }

        public bool HasExistingTransaction => !_ownsTransaction;
        public int CommitCount => _commitCount;
        public int RollbackCount => _rollbackCount;
        public bool Disposed { get; private set; }

        public ValueTask CommitAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_ownsTransaction)
            {
                Interlocked.Increment(ref _commitCount);
                _owner.CommitCount++;
            }

            return ValueTask.CompletedTask;
        }

        public ValueTask RollbackAsync(CancellationToken cancellationToken = default)
        {
            if (_ownsTransaction)
            {
                Interlocked.Increment(ref _rollbackCount);
                _owner.RollbackCount++;
            }

            return ValueTask.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            if (!Disposed)
            {
                Disposed = true;
                _owner.ActiveScopes--;
                _owner.DisposeCount++;
            }

            return ValueTask.CompletedTask;
        }
    }

    private sealed class RecordingTransactionManager : ITransactionManager
    {
        private int _beginCount;
        private readonly bool _joinWhenActive;
        private readonly Func<TransactionIsolationLevel, CancellationToken, ValueTask<ITransactionScope>>? _beginOverride;

        public RecordingTransactionManager(
            bool joinWhenActive = false,
            Func<TransactionIsolationLevel, CancellationToken, ValueTask<ITransactionScope>>? beginOverride = null)
        {
            _joinWhenActive = joinWhenActive;
            _beginOverride = beginOverride;
        }

        public int BeginCount => _beginCount;
        public int CommitCount { get; set; }
        public int RollbackCount { get; set; }
        public int DisposeCount { get; set; }
        public int ActiveScopes { get; set; }
        public List<TransactionIsolationLevel> IsolationLevels { get; } = [];
        public ConcurrentDictionary<int, byte> ConcurrentBeginThreadIds { get; } = new();

        public async ValueTask<ITransactionScope> BeginAsync(
            TransactionIsolationLevel isolationLevel,
            CancellationToken cancellationToken = default)
        {
            if (_beginOverride is not null)
            {
                return await _beginOverride(isolationLevel, cancellationToken).ConfigureAwait(false);
            }

            cancellationToken.ThrowIfCancellationRequested();
            Interlocked.Increment(ref _beginCount);
            ConcurrentBeginThreadIds.TryAdd(Environment.CurrentManagedThreadId, 0);
            IsolationLevels.Add(isolationLevel);

            bool owns = !_joinWhenActive || ActiveScopes == 0;
            return new RecordingScope(this, ownsTransaction: owns);
        }
    }

    private sealed class IncompleteBeginManager : ITransactionManager
    {
        public int BeginStarts { get; private set; }
        public int CommitCount { get; private set; }
        public int RollbackCount { get; private set; }

        public async ValueTask<ITransactionScope> BeginAsync(
            TransactionIsolationLevel isolationLevel,
            CancellationToken cancellationToken = default)
        {
            BeginStarts++;
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return new Scope(this);
        }

        private sealed class Scope : ITransactionScope
        {
            private readonly IncompleteBeginManager _owner;

            public Scope(IncompleteBeginManager owner) => _owner = owner;

            public bool HasExistingTransaction => false;

            public async ValueTask CommitAsync(CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                _owner.CommitCount++;
            }

            public async ValueTask RollbackAsync(CancellationToken cancellationToken = default)
            {
                await Task.Yield();
                _owner.RollbackCount++;
            }

            public async ValueTask DisposeAsync() => await Task.Yield();
        }
    }

    private sealed class ThrowingRollbackScope : ITransactionScope
    {
        public bool HasExistingTransaction => false;
        public bool RollbackCalled { get; private set; }

        public ValueTask CommitAsync(CancellationToken cancellationToken = default)
            => ValueTask.CompletedTask;

        public ValueTask RollbackAsync(CancellationToken cancellationToken = default)
        {
            RollbackCalled = true;
            throw new InvalidOperationException("rollback-failed");
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private static TransactionBehavior<TRequest, TResponse> CreateBehavior<TRequest, TResponse>(
        ITransactionManager manager,
        PlaxionMediatorTransactionOptions? options = null)
        where TRequest : IRequest<TResponse>
    {
        options ??= new PlaxionMediatorTransactionOptions();
        return new TransactionBehavior<TRequest, TResponse>(manager, options);
    }

    [Fact]
    public void Constructor_Throws_On_Null_Dependencies()
    {
        RecordingTransactionManager manager = new();
        PlaxionMediatorTransactionOptions options = new();

        Assert.Throws<ArgumentNullException>(() => new TransactionBehavior<Ping, string>(null!, options));
        Assert.Throws<ArgumentNullException>(() => new TransactionBehavior<Ping, string>(manager, null!));
    }

    [Fact]
    public async Task Handle_Throws_On_Null_Request_Or_Next()
    {
        TransactionBehavior<Ping, string> behavior = CreateBehavior<Ping, string>(new RecordingTransactionManager());

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            behavior.Handle(null!, () => ValueTask.FromResult("ok"), CancellationToken.None).AsTask());

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            behavior.Handle(new Ping("x"), null!, CancellationToken.None).AsTask());
    }

    // (1) successful commit
    [Fact]
    public async Task Successful_Commit()
    {
        RecordingTransactionManager manager = new();
        TransactionBehavior<TxPing, string> behavior = CreateBehavior<TxPing, string>(manager);

        string result = await behavior.Handle(
            new TxPing("ok"),
            () => ValueTask.FromResult("done"),
            CancellationToken.None);

        Assert.Equal("done", result);
        Assert.Equal(1, manager.BeginCount);
        Assert.Equal(1, manager.CommitCount);
        Assert.Equal(0, manager.RollbackCount);
        Assert.Equal(1, manager.DisposeCount);
    }

    // (2) handler throws → rollback
    [Fact]
    public async Task Handler_Throws_Rolls_Back()
    {
        RecordingTransactionManager manager = new();
        TransactionBehavior<TxPing, string> behavior = CreateBehavior<TxPing, string>(manager);

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behavior.Handle(
                new TxPing("fail"),
                () => throw new InvalidOperationException("boom"),
                CancellationToken.None).AsTask());

        Assert.Equal("boom", ex.Message);
        Assert.Equal(1, manager.BeginCount);
        Assert.Equal(0, manager.CommitCount);
        Assert.Equal(1, manager.RollbackCount);
        Assert.Equal(1, manager.DisposeCount);
    }

    // (3) behavior before transaction throws — transaction never starts (unit: cancel before begin)
    [Fact]
    public async Task Cancellation_Before_Transaction_Begins()
    {
        RecordingTransactionManager manager = new();
        TransactionBehavior<TxPing, string> behavior = CreateBehavior<TxPing, string>(manager);
        using CancellationTokenSource cts = new();
        cts.Cancel();
        bool nextCalled = false;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            behavior.Handle(
                new TxPing("x"),
                () =>
                {
                    nextCalled = true;
                    return ValueTask.FromResult("nope");
                },
                cts.Token).AsTask());

        Assert.False(nextCalled);
        Assert.Equal(0, manager.BeginCount);
        Assert.Equal(0, manager.CommitCount);
        Assert.Equal(0, manager.RollbackCount);
    }

    // (4) behavior inside transaction throws → rollback
    [Fact]
    public async Task Behavior_Inside_Transaction_Throws_Rolls_Back()
    {
        RecordingTransactionManager manager = new();
        TransactionBehavior<TxPing, string> behavior = CreateBehavior<TxPing, string>(manager);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behavior.Handle(
                new TxPing("inner"),
                async () =>
                {
                    await Task.Yield();
                    throw new InvalidOperationException("inner-behavior");
                },
                CancellationToken.None).AsTask());

        Assert.Equal(1, manager.BeginCount);
        Assert.Equal(1, manager.RollbackCount);
        Assert.Equal(0, manager.CommitCount);
    }

    // (5) already covered by Cancellation_Before_Transaction_Begins

    // (6) cancellation during handler → rollback
    [Fact]
    public async Task Cancellation_During_Handler_Rolls_Back()
    {
        RecordingTransactionManager manager = new();
        TransactionBehavior<TxPing, string> behavior = CreateBehavior<TxPing, string>(manager);
        using CancellationTokenSource cts = new();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            behavior.Handle(
                new TxPing("cancel"),
                () =>
                {
                    cts.Cancel();
                    cts.Token.ThrowIfCancellationRequested();
                    return ValueTask.FromResult("nope");
                },
                cts.Token).AsTask());

        Assert.Equal(1, manager.BeginCount);
        Assert.Equal(1, manager.RollbackCount);
        Assert.Equal(0, manager.CommitCount);
    }

    // (7) retry interaction — fresh transaction per attempt
    [Fact]
    public async Task Retry_Interaction_Fresh_Transaction_Per_Attempt()
    {
        RecordingTransactionManager manager = new();
        PlaxionMediatorRetryOptions retryOptions = new()
        {
            MaxRetryAttempts = 2,
            BaseDelay = TimeSpan.Zero,
        };
        RecordingDelayProvider delay = new();
        RetryBehavior<RetryableTxPing, string> retry = new(retryOptions, delay);
        TransactionBehavior<RetryableTxPing, string> tx = CreateBehavior<RetryableTxPing, string>(manager);

        int attempts = 0;
        string result = await retry.Handle(
            new RetryableTxPing("flaky"),
            () => tx.Handle(
                new RetryableTxPing("flaky"),
                () =>
                {
                    attempts++;
                    if (attempts < 3)
                    {
                        throw new InvalidOperationException($"transient-{attempts}");
                    }

                    return ValueTask.FromResult("recovered");
                },
                CancellationToken.None),
            CancellationToken.None);

        Assert.Equal("recovered", result);
        Assert.Equal(3, attempts);
        Assert.Equal(3, manager.BeginCount);
        Assert.Equal(2, manager.RollbackCount);
        Assert.Equal(1, manager.CommitCount);
    }

    // (8) nested Send — join existing transaction
    [Fact]
    public async Task Nested_Send_Joins_Existing_Transaction()
    {
        RecordingTransactionManager manager = new(joinWhenActive: true);
        TransactionBehavior<TxPing, string> behavior = CreateBehavior<TxPing, string>(manager);

        string result = await behavior.Handle(
            new TxPing("outer"),
            async () =>
            {
                string inner = await behavior.Handle(
                    new TxPing("inner"),
                    () => ValueTask.FromResult("inner-ok"),
                    CancellationToken.None);
                return "outer-" + inner;
            },
            CancellationToken.None);

        Assert.Equal("outer-inner-ok", result);
        Assert.Equal(2, manager.BeginCount);
        // Only the owning outer scope commits.
        Assert.Equal(1, manager.CommitCount);
        Assert.Equal(0, manager.RollbackCount);
    }

    // (9) existing transaction join semantics via HasExistingTransaction
    [Fact]
    public async Task Existing_Transaction_Join_Semantics()
    {
        RecordingTransactionManager manager = new(joinWhenActive: true);
        // Pre-open an outer scope so the next begin joins.
        ITransactionScope outer = await manager.BeginAsync(TransactionIsolationLevel.ReadCommitted);
        Assert.False(outer.HasExistingTransaction);

        TransactionBehavior<TxPing, string> behavior = CreateBehavior<TxPing, string>(manager);
        string result = await behavior.Handle(
            new TxPing("join"),
            () => ValueTask.FromResult("joined"),
            CancellationToken.None);

        Assert.Equal("joined", result);
        Assert.Equal(2, manager.BeginCount);
        // Joining scope must not commit/rollback the outer transaction.
        Assert.Equal(0, manager.CommitCount);
        Assert.Equal(0, manager.RollbackCount);

        await outer.CommitAsync();
        await outer.DisposeAsync();
        Assert.Equal(1, manager.CommitCount);
    }

    // (10) concurrent requests — independent transactions
    [Fact]
    public async Task Concurrent_Requests_Get_Independent_Transactions()
    {
        RecordingTransactionManager manager = new();
        TransactionBehavior<TxPing, string> behavior = CreateBehavior<TxPing, string>(manager);

        Task[] tasks = Enumerable.Range(0, 20)
            .Select(i => behavior.Handle(
                new TxPing($"c-{i}"),
                async () =>
                {
                    await Task.Yield();
                    return $"ok-{i}";
                },
                CancellationToken.None).AsTask())
            .ToArray();

        await Task.WhenAll(tasks);

        Assert.Equal(20, manager.BeginCount);
        Assert.Equal(20, manager.CommitCount);
        Assert.Equal(0, manager.RollbackCount);
    }

    // (11) scoped lifetime — manager survives throughout the scope
    [Fact]
    public void Scoped_Lifetime_Manager_Survives_Throughout_Scope()
    {
        ServiceCollection services = new();
        services.AddPlaxionMediatorTransactions();
        services.AddPlaxionMediatorTransactionManager<ScopedRecordingManager>(ServiceLifetime.Scoped);

        using ServiceProvider sp = services.BuildServiceProvider();
        using IServiceScope scope = sp.CreateScope();

        ITransactionManager first = scope.ServiceProvider.GetRequiredService<ITransactionManager>();
        ITransactionManager second = scope.ServiceProvider.GetRequiredService<ITransactionManager>();
        Assert.Same(first, second);

        using IServiceScope other = sp.CreateScope();
        ITransactionManager third = other.ServiceProvider.GetRequiredService<ITransactionManager>();
        Assert.NotSame(first, third);
    }

    private sealed class ScopedRecordingManager : ITransactionManager
    {
        public ValueTask<ITransactionScope> BeginAsync(
            TransactionIsolationLevel isolationLevel,
            CancellationToken cancellationToken = default)
            => new(new NoOpScope());
    }

    private sealed class NoOpScope : ITransactionScope
    {
        public bool HasExistingTransaction => false;
        public ValueTask CommitAsync(CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask RollbackAsync(CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    // (12) transient transaction manager
    [Fact]
    public void Transient_Manager_Resolved_Per_Request()
    {
        ServiceCollection services = new();
        services.AddPlaxionMediatorTransactions();
        services.AddPlaxionMediatorTransactionManager<ScopedRecordingManager>(ServiceLifetime.Transient);

        using ServiceProvider sp = services.BuildServiceProvider();
        ITransactionManager first = sp.GetRequiredService<ITransactionManager>();
        ITransactionManager second = sp.GetRequiredService<ITransactionManager>();
        Assert.NotSame(first, second);
    }

    // (13) multiple sequential requests — each gets its own boundary
    [Fact]
    public async Task Multiple_Sequential_Requests_Each_Get_Own_Boundary()
    {
        RecordingTransactionManager manager = new();
        TransactionBehavior<TxPing, string> behavior = CreateBehavior<TxPing, string>(manager);

        for (int i = 0; i < 5; i++)
        {
            string result = await behavior.Handle(
                new TxPing($"seq-{i}"),
                () => ValueTask.FromResult($"ok-{i}"),
                CancellationToken.None);
            Assert.Equal($"ok-{i}", result);
        }

        Assert.Equal(5, manager.BeginCount);
        Assert.Equal(5, manager.CommitCount);
        Assert.Equal(5, manager.DisposeCount);
    }

    // (14) async incomplete ValueTask path
    [Fact]
    public async Task Async_Incomplete_ValueTask_Path()
    {
        IncompleteBeginManager manager = new();
        TransactionBehavior<TxPing, string> behavior = CreateBehavior<TxPing, string>(manager);

        string result = await behavior.Handle(
            new TxPing("async"),
            async () =>
            {
                await Task.Yield();
                return "async-ok";
            },
            CancellationToken.None);

        Assert.Equal("async-ok", result);
        Assert.Equal(1, manager.BeginStarts);
        Assert.Equal(1, manager.CommitCount);
        Assert.Equal(0, manager.RollbackCount);
    }

    // (15) exception wrapping semantics — original exception preserved
    [Fact]
    public async Task Exception_Wrapping_Preserves_Original_Exception()
    {
        ThrowingRollbackScope scope = new();
        RecordingTransactionManager manager = new(beginOverride: (_, _) => new ValueTask<ITransactionScope>(scope));
        TransactionBehavior<TxPing, string> behavior = CreateBehavior<TxPing, string>(manager);

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behavior.Handle(
                new TxPing("x"),
                () => throw new InvalidOperationException("handler-fault"),
                CancellationToken.None).AsTask());

        Assert.Equal("handler-fault", ex.Message);
        Assert.True(scope.RollbackCalled);
    }

    // (16) no transaction requested — fast path
    [Fact]
    public async Task NonTransactional_Request_Is_Fast_Path_NoOp()
    {
        RecordingTransactionManager manager = new();
        TransactionBehavior<Ping, string> behavior = CreateBehavior<Ping, string>(manager);
        int calls = 0;

        string result = await behavior.Handle(
            new Ping("plain"),
            () =>
            {
                calls++;
                return ValueTask.FromResult("ok");
            },
            CancellationToken.None);

        Assert.Equal("ok", result);
        Assert.Equal(1, calls);
        Assert.Equal(0, manager.BeginCount);
    }

    // (17) behavior registered but request not transactional
    [Fact]
    public async Task Registered_But_NonTransactional_Does_Not_Begin()
    {
        RecordingTransactionManager manager = new();
        TransactionBehavior<Ping, string> behavior = CreateBehavior<Ping, string>(manager);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behavior.Handle(
                new Ping("no-tx"),
                () => throw new InvalidOperationException("handler"),
                CancellationToken.None).AsTask());

        Assert.Equal(0, manager.BeginCount);
        Assert.Equal(0, manager.RollbackCount);
    }

    [Fact]
    public async Task Uses_Default_Isolation_When_Request_Does_Not_Override()
    {
        RecordingTransactionManager manager = new();
        PlaxionMediatorTransactionOptions options = new()
        {
            DefaultIsolationLevel = TransactionIsolationLevel.Serializable,
        };
        TransactionBehavior<TxPing, string> behavior = CreateBehavior<TxPing, string>(manager, options);

        await behavior.Handle(new TxPing("x"), () => ValueTask.FromResult("ok"), CancellationToken.None);

        Assert.Equal(new[] { TransactionIsolationLevel.Serializable }, manager.IsolationLevels);
    }

    [Fact]
    public async Task Per_Request_Isolation_Overrides_Options()
    {
        RecordingTransactionManager manager = new();
        PlaxionMediatorTransactionOptions options = new()
        {
            DefaultIsolationLevel = TransactionIsolationLevel.ReadCommitted,
        };
        TransactionBehavior<TxPing, string> behavior = CreateBehavior<TxPing, string>(manager, options);

        await behavior.Handle(
            new TxPing("x") { IsolationLevel = TransactionIsolationLevel.Snapshot },
            () => ValueTask.FromResult("ok"),
            CancellationToken.None);

        Assert.Equal(new[] { TransactionIsolationLevel.Snapshot }, manager.IsolationLevels);
    }

    [Fact]
    public void ServiceCollection_Registers_Options_And_Behavior()
    {
        ServiceCollection services = new();
        services.AddPlaxionMediatorTransactions(o => o.DefaultIsolationLevel = TransactionIsolationLevel.RepeatableRead);
        services.AddSingleton<ITransactionManager, RecordingTransactionManager>();

        using ServiceProvider sp = services.BuildServiceProvider();
        PlaxionMediatorTransactionOptions options = sp.GetRequiredService<PlaxionMediatorTransactionOptions>();
        Assert.Equal(TransactionIsolationLevel.RepeatableRead, options.DefaultIsolationLevel);

        IEnumerable<IPipelineBehavior<TxPing, string>> behaviors =
            sp.GetServices<IPipelineBehavior<TxPing, string>>();
        Assert.Contains(behaviors, b => b is TransactionBehavior<TxPing, string>);
    }

    [Fact]
    public void UsePlaxionMediatorTransactionBehavior_Adds_GlobalBehavior_Once()
    {
        PlaxionMediatorOptions options = new();
        options.UsePlaxionMediatorTransactionBehavior();
        options.UsePlaxionMediatorTransactionBehavior();

        Assert.Single(options.GlobalBehaviors);
        Assert.Equal(typeof(TransactionBehavior<,>), options.GlobalBehaviors[0]);
    }

    private sealed class RecordingDelayProvider : IRetryDelayProvider
    {
        public ValueTask DelayAsync(TimeSpan delay, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.CompletedTask;
        }
    }
}

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Microsoft.Extensions.DependencyInjection;
using PlaxionMediator.Abstractions;
using PlaxionMediator.Core;
using PlaxionMediator.Transactions;

namespace PlaxionMediator.Benchmarks;

/// <summary>
/// Transaction pipeline overhead benchmarks. Does not modify the core Send hot path —
/// measures only TransactionBehavior / ITransactionManager costs when opted in.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class TransactionBenchmarks
{
    private ServiceProvider _baselineProvider = null!;
    private ServiceProvider _fastPathProvider = null!;
    private ServiceProvider _noopTxProvider = null!;
    private ISender _baselineSender = null!;
    private ISender _fastPathSender = null!;
    private ISender _noopTxSender = null!;
    private PlainPing _plainPing = null!;
    private TxPing _txPing = null!;
    private TxFailPing _txFailPing = null!;

    [GlobalSetup]
    public void Setup()
    {
        _plainPing = new PlainPing("benchmark");
        _txPing = new TxPing("benchmark");
        _txFailPing = new TxFailPing("benchmark");

        // 1) Send without transaction behavior at all
        ServiceCollection baseline = new();
        baseline.AddPlaxionMediator();
        _baselineProvider = baseline.BuildServiceProvider();
        _baselineSender = _baselineProvider.GetRequiredService<ISender>();

        // 2) TransactionBehavior registered, non-transactional request (fast path)
        ServiceCollection fastPath = new();
        fastPath.AddPlaxionMediator(o => o.UsePlaxionMediatorTransactionBehavior());
        fastPath.AddPlaxionMediatorTransactions();
        fastPath.AddSingleton<ITransactionManager, NoOpTransactionManager>();
        _fastPathProvider = fastPath.BuildServiceProvider();
        _fastPathSender = _fastPathProvider.GetRequiredService<ISender>();

        // 3–5) Transactional send with no-op manager (commit + rollback paths)
        ServiceCollection noopTx = new();
        noopTx.AddPlaxionMediator(o => o.UsePlaxionMediatorTransactionBehavior());
        noopTx.AddPlaxionMediatorTransactions();
        noopTx.AddSingleton<ITransactionManager, NoOpTransactionManager>();
        _noopTxProvider = noopTx.BuildServiceProvider();
        _noopTxSender = _noopTxProvider.GetRequiredService<ISender>();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _baselineProvider.Dispose();
        _fastPathProvider.Dispose();
        _noopTxProvider.Dispose();
    }

    [Benchmark(Baseline = true, Description = "Send_NoTransaction")]
    public ValueTask<string> Send_NoTransaction()
        => _baselineSender.Send(_plainPing);

    [Benchmark(Description = "Send_TransactionBehavior_NonTransactional_FastPath")]
    public ValueTask<string> Send_TransactionBehavior_NonTransactional_FastPath()
        => _fastPathSender.Send(_plainPing);

    [Benchmark(Description = "Send_Transactional_NoOpManager")]
    public ValueTask<string> Send_Transactional_NoOpManager()
        => _noopTxSender.Send(_txPing);

    [Benchmark(Description = "Send_Transactional_CommitPath")]
    public ValueTask<string> Send_Transactional_CommitPath()
        => _noopTxSender.Send(_txPing);

    [Benchmark(Description = "Send_Transactional_RollbackPath")]
    public async Task Send_Transactional_RollbackPath()
    {
        try
        {
            await _noopTxSender.Send(_txFailPing);
        }
        catch (InvalidOperationException)
        {
            // expected
        }
    }
}

public sealed record PlainPing(string Message) : IRequest<string>;

public sealed class PlainPingHandler : IRequestHandler<PlainPing, string>
{
    public ValueTask<string> Handle(PlainPing request, CancellationToken cancellationToken)
        => ValueTask.FromResult("Pong:" + request.Message);
}

public sealed record TxPing(string Message) : IRequest<string>, ITransactionalRequest;

public sealed class TxPingHandler : IRequestHandler<TxPing, string>
{
    public ValueTask<string> Handle(TxPing request, CancellationToken cancellationToken)
        => ValueTask.FromResult("Pong:" + request.Message);
}

public sealed record TxFailPing(string Message) : IRequest<string>, ITransactionalRequest;

public sealed class TxFailPingHandler : IRequestHandler<TxFailPing, string>
{
    public ValueTask<string> Handle(TxFailPing request, CancellationToken cancellationToken)
        => throw new InvalidOperationException("benchmark-rollback");
}

public sealed class NoOpTransactionManager : ITransactionManager
{
    public ValueTask<ITransactionScope> BeginAsync(
        TransactionIsolationLevel isolationLevel,
        CancellationToken cancellationToken = default)
        => new(new NoOpScope());

    private sealed class NoOpScope : ITransactionScope
    {
        public bool HasExistingTransaction => false;
        public ValueTask CommitAsync(CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask RollbackAsync(CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}

using PlaxionMediator.Analyzers;
using Xunit;

namespace PlaxionMediator.Analyzers.Tests;

public sealed class MissingTransactionBehaviorAnalyzerTests
{
    [Fact]
    public async Task TransactionalRequest_MissingBehavior_ReportsError()
    {
        const string source = """
            using PlaxionMediator.Abstractions;
            using PlaxionMediator.Transactions;
            public sealed record CreateOrderRequest(string CustomerId) : IRequest<string>, ITransactionalRequest;
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new MissingTransactionBehaviorAnalyzer(), source);
        Assert.Contains(diagnostics, d => d.Id == "PlaxionMediator042");
    }

    [Fact]
    public async Task TransactionalRequest_WithBehavior_NoDiagnostic()
    {
        const string source = """
            using PlaxionMediator.Abstractions;
            using PlaxionMediator.Transactions;
            using PlaxionMediator;
            public sealed record CreateOrderRequest(string CustomerId) : IRequest<string>, ITransactionalRequest;
            public static class Startup
            {
                public static void Configure(PlaxionMediatorOptions o)
                {
                    o.UsePlaxionMediatorTransactionBehavior();
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new MissingTransactionBehaviorAnalyzer(), source);
        Assert.DoesNotContain(diagnostics, d => d.Id == "PlaxionMediator042");
    }
}

public sealed class InvalidTransactionOrderingAnalyzerTests
{
    [Fact]
    public async Task RetryInsideTransaction_ReportsError()
    {
        // Transaction registered before Retry => Transaction outer, Retry inner (unsafe)
        const string source = """
            using PlaxionMediator;
            using PlaxionMediator.Retry;
            using PlaxionMediator.Transactions;
            public static class Startup
            {
                public static void Configure(PlaxionMediatorOptions o)
                {
                    o.UsePlaxionMediatorTransactionBehavior();
                    o.UsePlaxionMediatorRetryBehavior();
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new InvalidTransactionOrderingAnalyzer(), source);
        Assert.Contains(diagnostics, d => d.Id == "PlaxionMediator043");
    }

    [Fact]
    public async Task TransactionInsideRetry_NoDiagnostic()
    {
        // Retry registered before Transaction => Retry outer, Transaction inner (safe / recommended)
        const string source = """
            using PlaxionMediator;
            using PlaxionMediator.Retry;
            using PlaxionMediator.Transactions;
            public static class Startup
            {
                public static void Configure(PlaxionMediatorOptions o)
                {
                    o.UsePlaxionMediatorRetryBehavior();
                    o.UsePlaxionMediatorTransactionBehavior();
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new InvalidTransactionOrderingAnalyzer(), source);
        Assert.DoesNotContain(diagnostics, d => d.Id == "PlaxionMediator043");
    }
}

public sealed class AmbiguousTransactionManagerAnalyzerTests
{
    [Fact]
    public async Task MultipleManagers_ReportsError()
    {
        const string source = """
            using Microsoft.Extensions.DependencyInjection;
            using PlaxionMediator.Transactions;
            public sealed class MgrA : ITransactionManager
            {
                public System.Threading.Tasks.ValueTask<ITransactionScope> BeginAsync(
                    TransactionIsolationLevel isolationLevel,
                    System.Threading.CancellationToken cancellationToken = default)
                    => throw new System.NotImplementedException();
            }
            public sealed class MgrB : ITransactionManager
            {
                public System.Threading.Tasks.ValueTask<ITransactionScope> BeginAsync(
                    TransactionIsolationLevel isolationLevel,
                    System.Threading.CancellationToken cancellationToken = default)
                    => throw new System.NotImplementedException();
            }
            public static class Startup
            {
                public static void Configure(IServiceCollection services)
                {
                    services.AddScoped<ITransactionManager, MgrA>();
                    services.AddScoped<ITransactionManager, MgrB>();
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new AmbiguousTransactionManagerAnalyzer(), source);
        Assert.Contains(diagnostics, d => d.Id == "PlaxionMediator044");
    }

    [Fact]
    public async Task SingleManager_NoDiagnostic()
    {
        const string source = """
            using Microsoft.Extensions.DependencyInjection;
            using PlaxionMediator.Transactions;
            public sealed class MgrA : ITransactionManager
            {
                public System.Threading.Tasks.ValueTask<ITransactionScope> BeginAsync(
                    TransactionIsolationLevel isolationLevel,
                    System.Threading.CancellationToken cancellationToken = default)
                    => throw new System.NotImplementedException();
            }
            public static class Startup
            {
                public static void Configure(IServiceCollection services)
                {
                    services.AddScoped<ITransactionManager, MgrA>();
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new AmbiguousTransactionManagerAnalyzer(), source);
        Assert.DoesNotContain(diagnostics, d => d.Id == "PlaxionMediator044");
    }
}

public sealed class UnsupportedIsolationLevelAnalyzerTests
{
    [Fact]
    public async Task UnsupportedIsolation_ReportsWarning()
    {
        const string source = """
            using Microsoft.EntityFrameworkCore;
            using Microsoft.Extensions.DependencyInjection;
            using PlaxionMediator.Abstractions;
            using PlaxionMediator.Transactions;
            public sealed class AppDbContext : DbContext
            {
                public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
            }
            public sealed record SnapshotOrderRequest(string Id) : IRequest<string>, ITransactionalRequest
            {
                public TransactionIsolationLevel? IsolationLevel => TransactionIsolationLevel.Snapshot;
            }
            public static class Startup
            {
                public static void Configure(IServiceCollection services)
                {
                    services.AddDbContext<AppDbContext>(o => o.UseSqlite("Data Source=test.db"));
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new UnsupportedIsolationLevelAnalyzer(), source);
        Assert.Contains(diagnostics, d => d.Id == "PlaxionMediator045");
    }

    [Fact]
    public async Task SupportedIsolation_NoDiagnostic()
    {
        const string source = """
            using Microsoft.EntityFrameworkCore;
            using Microsoft.Extensions.DependencyInjection;
            using PlaxionMediator.Abstractions;
            using PlaxionMediator.Transactions;
            public sealed class AppDbContext : DbContext
            {
                public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
            }
            public sealed record OrderRequest(string Id) : IRequest<string>, ITransactionalRequest
            {
                public TransactionIsolationLevel? IsolationLevel => TransactionIsolationLevel.ReadCommitted;
            }
            public static class Startup
            {
                public static void Configure(IServiceCollection services)
                {
                    services.AddDbContext<AppDbContext>(o => o.UseSqlite("Data Source=test.db"));
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(new UnsupportedIsolationLevelAnalyzer(), source);
        Assert.DoesNotContain(diagnostics, d => d.Id == "PlaxionMediator045");
    }
}

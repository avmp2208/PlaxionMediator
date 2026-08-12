using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlaxionMediator.Abstractions;
using PlaxionMediator.Core;
using Xunit;

namespace PlaxionMediator.Transactions.EntityFrameworkCore.Tests;

// Top-level types so the source generator can discover the handler for ISender integration tests.
public sealed class Order
{
    public Guid Id { get; set; }
    public string CustomerId { get; set; } = "";
    public decimal Total { get; set; }
}

public sealed class SampleDbContext : DbContext
{
    public SampleDbContext(DbContextOptions<SampleDbContext> options) : base(options)
    {
    }

    public DbSet<Order> Orders => Set<Order>();
}

public sealed record CreateOrderRequest(string CustomerId, decimal Total)
    : IRequest<Guid>, ITransactionalRequest;

public sealed class FailFlag
{
    public bool ShouldFail { get; set; }
}

public sealed class CreateOrderHandler : IRequestHandler<CreateOrderRequest, Guid>
{
    private readonly SampleDbContext _db;
    private readonly FailFlag _failFlag;

    public CreateOrderHandler(SampleDbContext db, FailFlag failFlag)
    {
        _db = db;
        _failFlag = failFlag;
    }

    public async ValueTask<Guid> Handle(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        Order order = new()
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            Total = request.Total,
        };
        _db.Orders.Add(order);
        await _db.SaveChangesAsync(cancellationToken);

        if (_failFlag.ShouldFail)
        {
            throw new InvalidOperationException("simulated-handler-failure");
        }

        return order.Id;
    }
}

// (18) EF Core adapter integration test
public sealed class EfCoreTransactionManagerTests
{
    private static async Task<(ServiceProvider Sp, SqliteConnection Connection)> CreateSqliteProviderAsync(
        bool shouldFail = false)
    {
        // Keep the in-memory SQLite database alive for the lifetime of the connection.
        SqliteConnection connection = new("DataSource=:memory:");
        await connection.OpenAsync();

        ServiceCollection services = new();
        services.AddDbContext<SampleDbContext>(o => o.UseSqlite(connection));
        services.AddSingleton(new FailFlag { ShouldFail = shouldFail });
        services.AddPlaxionMediator(o => o.UsePlaxionMediatorTransactionBehavior());
        services.AddPlaxionMediatorTransactionsEntityFrameworkCore<SampleDbContext>();

        ServiceProvider sp = services.BuildServiceProvider();
        using (IServiceScope scope = sp.CreateScope())
        {
            SampleDbContext db = scope.ServiceProvider.GetRequiredService<SampleDbContext>();
            await db.Database.EnsureCreatedAsync();
        }

        return (sp, connection);
    }

    [Fact]
    public async Task Commit_Persists_Order()
    {
        (ServiceProvider sp, SqliteConnection connection) = await CreateSqliteProviderAsync();
        await using (sp)
        await using (connection)
        {
            using IServiceScope scope = sp.CreateScope();
            ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();

            Guid id = await sender.Send(new CreateOrderRequest("cust-1", 42.5m));

            SampleDbContext db = scope.ServiceProvider.GetRequiredService<SampleDbContext>();
            Order? order = await db.Orders.SingleOrDefaultAsync(o => o.Id == id);
            Assert.NotNull(order);
            Assert.Equal("cust-1", order!.CustomerId);
            Assert.Equal(42.5m, order.Total);
        }
    }

    [Fact]
    public async Task Handler_Failure_Rolls_Back_Order()
    {
        (ServiceProvider sp, SqliteConnection connection) = await CreateSqliteProviderAsync(shouldFail: true);
        await using (sp)
        await using (connection)
        {
            using IServiceScope scope = sp.CreateScope();
            ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                sender.Send(new CreateOrderRequest("cust-2", 10m)).AsTask());

            // New scope/context to observe committed state on the shared connection.
            using IServiceScope verifyScope = sp.CreateScope();
            SampleDbContext db = verifyScope.ServiceProvider.GetRequiredService<SampleDbContext>();
            Assert.Equal(0, await db.Orders.CountAsync());
        }
    }

    [Fact]
    public async Task Join_Existing_Transaction_Does_Not_Nest()
    {
        SqliteConnection connection = new("DataSource=:memory:");
        await connection.OpenAsync();
        await using (connection)
        {
            DbContextOptions<SampleDbContext> options = new DbContextOptionsBuilder<SampleDbContext>()
                .UseSqlite(connection)
                .Options;

            await using SampleDbContext db = new(options);
            await db.Database.EnsureCreatedAsync();

            EfCoreTransactionManager<SampleDbContext> manager = new(db);

            await using ITransactionScope outer = await manager.BeginAsync(TransactionIsolationLevel.ReadCommitted);
            Assert.False(outer.HasExistingTransaction);

            await using ITransactionScope inner = await manager.BeginAsync(TransactionIsolationLevel.ReadCommitted);
            Assert.True(inner.HasExistingTransaction);

            db.Orders.Add(new Order { Id = Guid.NewGuid(), CustomerId = "join", Total = 1m });
            await db.SaveChangesAsync();

            // Inner join scope must not commit/rollback independently.
            await inner.CommitAsync();
            await outer.CommitAsync();

            Assert.Equal(1, await db.Orders.CountAsync());
        }
    }

    [Fact]
    public async Task Manager_Never_Calls_SaveChanges_Implicitly_On_Empty_Transaction()
    {
        SqliteConnection connection = new("DataSource=:memory:");
        await connection.OpenAsync();
        await using (connection)
        {
            DbContextOptions<SampleDbContext> options = new DbContextOptionsBuilder<SampleDbContext>()
                .UseSqlite(connection)
                .Options;

            await using SampleDbContext db = new(options);
            await db.Database.EnsureCreatedAsync();
            EfCoreTransactionManager<SampleDbContext> manager = new(db);

            TransactionBehavior<CreateOrderRequest, Guid> behavior = new(
                manager,
                new PlaxionMediatorTransactionOptions());

            // Non-persisting next delegate: manager/behavior must not SaveChanges on their own.
            Guid result = await behavior.Handle(
                new CreateOrderRequest("x", 1m),
                () => ValueTask.FromResult(Guid.NewGuid()),
                CancellationToken.None);

            Assert.NotEqual(Guid.Empty, result);
            Assert.Equal(0, await db.Orders.CountAsync());
        }
    }

    [Fact]
    public void Di_Registers_Scoped_Manager()
    {
        ServiceCollection services = new();
        services.AddDbContext<SampleDbContext>(o => o.UseInMemoryDatabase("di-test"));
        services.AddPlaxionMediatorTransactionsEntityFrameworkCore<SampleDbContext>();

        using ServiceProvider sp = services.BuildServiceProvider();
        using IServiceScope scope = sp.CreateScope();
        ITransactionManager manager = scope.ServiceProvider.GetRequiredService<ITransactionManager>();
        Assert.IsType<EfCoreTransactionManager<SampleDbContext>>(manager);
        Assert.NotNull(scope.ServiceProvider.GetService<PlaxionMediatorTransactionOptions>());
    }
}

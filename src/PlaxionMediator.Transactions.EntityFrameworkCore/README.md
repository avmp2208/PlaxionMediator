# PlaxionMediator.Transactions.EntityFrameworkCore

Entity Framework Core adapter for `PlaxionMediator.Transactions`. Registers
`EfCoreTransactionManager<TDbContext>` as `ITransactionManager`, wrapping
`Database.BeginTransactionAsync` / commit / rollback.

**Never** calls `SaveChanges` — handlers remain responsible for persisting changes inside the
transaction boundary opened by `TransactionBehavior`.

## Install

```bash
dotnet add package PlaxionMediator.Transactions.EntityFrameworkCore
```

## Usage

```csharp
using PlaxionMediator.Transactions;
using PlaxionMediator.Transactions.EntityFrameworkCore;

services.AddDbContext<AppDbContext>(/* ... */);

services.AddPlaxionMediator(o =>
{
    o.UsePlaxionMediatorValidationBehavior();
    o.UsePlaxionMediatorRetryBehavior();
    o.UsePlaxionMediatorTransactionBehavior(); // after Retry
});

services.AddPlaxionMediatorTransactionsEntityFrameworkCore<AppDbContext>();
```

```csharp
public sealed class CreateOrderHandler : IRequestHandler<CreateOrderRequest, OrderDto>
{
    private readonly AppDbContext _db;

    public CreateOrderHandler(AppDbContext db) => _db = db;

    public async ValueTask<OrderDto> Handle(CreateOrderRequest request, CancellationToken ct)
    {
        var order = new Order { /* ... */ };
        _db.Orders.Add(order);
        await _db.SaveChangesAsync(ct); // handler owns SaveChanges
        return new OrderDto(order.Id, order.CustomerId, order.Total);
    }
}
```

## Learn more

See [`docs/wiki/Transactions.md`](https://github.com/avmp2208/PlaxionMediator/blob/master/docs/wiki/Transactions.md).

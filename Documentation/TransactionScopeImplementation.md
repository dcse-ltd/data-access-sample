You're on the right track! Here's how to properly integrate transaction management with the ASP.NET pipeline and MediatR:

## Recommended Approach: Pipeline Behaviors

Use **MediatR Pipeline Behaviors** for transaction management. This gives you:
- Automatic transaction handling around requests that need it
- Clean separation from business logic
- Attribute-based or convention-based control
- Works perfectly with the .NET request pipeline

## 1. Create a Transaction Behavior for MediatR

```csharp
using MediatR;
using DataAccess.UnitOfWork.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Behaviors;

/// <summary>
/// MediatR pipeline behavior that wraps requests in a database transaction
/// </summary>
public class TransactionBehavior<TRequest, TResponse>(
    IUnitOfWork unitOfWork,
    ILogger<TransactionBehavior<TRequest, TResponse>> logger) 
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Skip if already in a transaction (nested calls)
        if (unitOfWork.HasActiveTransaction)
        {
            return await next();
        }

        // Check if this request type should use transactions
        if (!ShouldUseTransaction(request))
        {
            return await next();
        }

        var requestName = typeof(TRequest).Name;
        logger.LogDebug("Beginning transaction for {RequestName}", requestName);

        try
        {
            await unitOfWork.BeginTransactionAsync(cancellationToken);
            
            var response = await next();
            
            await unitOfWork.CommitTransactionAsync(cancellationToken);
            logger.LogDebug("Transaction committed for {RequestName}", requestName);
            
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Transaction failed for {RequestName}, rolling back", requestName);
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    private static bool ShouldUseTransaction(TRequest request)
    {
        // Option 1: Use marker interface
        if (request is ITransactionalRequest)
            return true;

        // Option 2: Use attribute
        if (typeof(TRequest).GetCustomAttributes(typeof(TransactionalAttribute), true).Any())
            return true;

        // Option 3: Convention-based (any Command uses transaction)
        if (typeof(TRequest).Name.EndsWith("Command"))
            return true;

        return false;
    }
}
```

## 2. Create Marker Interface and Attribute

```csharp
namespace Infrastructure.Behaviors;

/// <summary>
/// Marker interface to indicate a request should run within a transaction
/// </summary>
public interface ITransactionalRequest
{
}

/// <summary>
/// Attribute to mark requests that should run within a transaction
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class TransactionalAttribute : Attribute
{
}
```

## 3. Register the Behavior

```csharp
// In Program.cs or Startup.cs
services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    
    // Add the transaction behavior to the pipeline
    cfg.AddOpenBehavior(typeof(TransactionBehavior<,>));
    
    // Optional: Add other behaviors
    // cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
    // cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
});
```

## 4. Usage Examples

### Option A: Using Marker Interface

```csharp
public record CreateOrderCommand(
    Guid CustomerId,
    List<OrderItemDto> Items) : IRequest<OrderDto>, ITransactionalRequest;

public class CreateOrderCommandHandler(
    ICoreEntityService<Order> orderService,
    ICoreEntityService<OrderProduct> orderProductService,
    IMapper mapper) : IRequestHandler<CreateOrderCommand, OrderDto>
{
    public async Task<OrderDto> Handle(
        CreateOrderCommand request,
        CancellationToken cancellationToken)
    {
        // This entire operation runs in a transaction automatically
        var order = new Order
        {
            CustomerId = request.CustomerId
        };
        
        var createdOrder = await orderService.CreateAsync(order, cancellationToken);
        
        // Add order items (multiple SaveChanges calls)
        foreach (var item in request.Items)
        {
            var orderProduct = mapper.Map<OrderProduct>(item);
            orderProduct.OrderId = createdOrder.Id;
            await orderProductService.CreateAsync(orderProduct, cancellationToken);
        }
        
        return mapper.Map<OrderDto>(createdOrder);
    }
}
```

### Option B: Using Attribute

```csharp
[Transactional]
public record UpdateOrderCommand(
    Guid OrderId,
    List<OrderItemDto> Items) : IRequest<OrderDto>;
```

### Option C: Convention-Based (Automatic for Commands)

```csharp
// Any class ending with "Command" automatically gets a transaction
public record DeleteCustomerCommand(Guid CustomerId) : IRequest;

// Queries don't get transactions
public record GetCustomerQuery(Guid CustomerId) : IRequest<CustomerDto>;
```

## 5. For Non-MediatR Scenarios (Middleware)

If you need transaction support before MediatR or for non-MediatR endpoints:

```csharp
public class TransactionMiddleware(
    RequestDelegate next,
    ILogger<TransactionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, IUnitOfWork unitOfWork)
    {
        // Only wrap POST, PUT, PATCH, DELETE in transactions
        if (context.Request.Method == HttpMethods.Get || 
            context.Request.Method == HttpMethods.Head)
        {
            await next(context);
            return;
        }

        // Check for opt-out header or attribute on controller/action
        if (ShouldSkipTransaction(context))
        {
            await next(context);
            return;
        }

        try
        {
            await unitOfWork.BeginTransactionAsync(context.RequestAborted);
            
            await next(context);
            
            if (context.Response.StatusCode < 400)
            {
                await unitOfWork.CommitTransactionAsync(context.RequestAborted);
            }
            else
            {
                await unitOfWork.RollbackTransactionAsync(context.RequestAborted);
            }
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(context.RequestAborted);
            throw;
        }
    }

    private bool ShouldSkipTransaction(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        return endpoint?.Metadata.GetMetadata<NoTransactionAttribute>() != null;
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class NoTransactionAttribute : Attribute { }

// Register in Program.cs
app.UseMiddleware<TransactionMiddleware>();
```

## 6. Advanced: Scoped Transaction Control

For complex scenarios where services need transaction control:

```csharp
public interface ITransactionScope
{
    Task ExecuteInTransactionAsync(
        Func<Task> action,
        CancellationToken cancellationToken = default);
    
    Task<T> ExecuteInTransactionAsync<T>(
        Func<Task<T>> action,
        CancellationToken cancellationToken = default);
}

public class TransactionScope(IUnitOfWork unitOfWork) : ITransactionScope
{
    public async Task ExecuteInTransactionAsync(
        Func<Task> action,
        CancellationToken cancellationToken = default)
    {
        var hasExistingTransaction = unitOfWork.HasActiveTransaction;
        
        if (!hasExistingTransaction)
            await unitOfWork.BeginTransactionAsync(cancellationToken);
        
        try
        {
            await action();
            
            if (!hasExistingTransaction)
                await unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            if (!hasExistingTransaction)
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        var hasExistingTransaction = unitOfWork.HasActiveTransaction;
        
        if (!hasExistingTransaction)
            await unitOfWork.BeginTransactionAsync(cancellationToken);
        
        try
        {
            var result = await action();
            
            if (!hasExistingTransaction)
                await unitOfWork.CommitTransactionAsync(cancellationToken);
            
            return result;
        }
        catch
        {
            if (!hasExistingTransaction)
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
```

## Recommended Strategy

**Primary: MediatR Pipeline Behavior** (when using MediatR)
- Use convention-based (Commands = transactions) for simplicity
- Override with `ITransactionalRequest` interface when queries need transactions
- This is the cleanest, most maintainable approach

**Fallback: Middleware** (for non-MediatR endpoints)
- Use for legacy controllers or minimal APIs
- Automatically wraps mutating operations

**Manual: TransactionScope** (for special cases)
- Use when you need fine-grained control
- Good for background jobs or complex orchestration

## Key Benefits

1. ✅ **Separation of Concerns**: Business logic doesn't know about transactions
2. ✅ **DRY**: Transaction logic in one place
3. ✅ **Testable**: Easy to test handlers without transaction overhead
4. ✅ **Flexible**: Multiple ways to opt-in/out
5. ✅ **Safe**: Prevents nested transaction issues
6. ✅ **MediatR Ready**: Works perfectly with your future architecture

This approach keeps your services clean while ensuring data consistency at the right level of abstraction.

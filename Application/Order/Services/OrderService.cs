using Application.Order.Dtos;
using Application.Order.Interfaces;
using AutoMapper;
using Infrastructure.Entity.Models;
using Infrastructure.Repository.Interfaces;
using Infrastructure.Repository.Models;
using Infrastructure.Services.Interfaces;
using Infrastructure.Services.Models;

namespace Application.Order.Services;

public class OrderService(
    ICoreEntityService<Entities.Order> coreEntityService,
    ICurrentUserService currentUserService,
    ICollectionSyncService collectionSyncService,
    IEntitySoftDeleteService<Entities.OrderProduct> orderProductSoftDeleteService,
    IRepository<Entities.OrderProduct> orderProductRepository,
    IMapper mapper) : IOrderService
{
    /// <summary>
    /// Lock options for Order entities that include cascading locks to child entities.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="LockOptions.IncludeChildren"/> is set to <c>true</c> to enable cascading locks
    /// to child entities (OrderProducts) when locking an Order.
    /// </para>
    /// <para>
    /// <see cref="LockOptions.MaxDepth"/> is set to <c>1</c> to lock the Order entity and its direct
    /// children (OrderProducts), but not deeper nested entities. This prevents deadlocks while
    /// ensuring all related entities that need to be modified together are locked.
    /// </para>
    /// </remarks>
    private static readonly LockOptions OrderLockOptions = new()
    {
        IncludeChildren = true,
        MaxDepth = 1 // Lock Order and its direct children (OrderProducts) only
    };
    
    public async Task<IEnumerable<OrderDto>> GetAllAsync(CancellationToken cancellationToken)
        => mapper.Map<IEnumerable<OrderDto>>(await coreEntityService.GetAllAsync(cancellationToken));

    public async Task<OrderDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => mapper.Map<OrderDto>(await coreEntityService.GetByIdAsync(id, cancellationToken));
    
    public async Task<OrderDto> GetByIdWithLockAsync(Guid id, CancellationToken cancellationToken = default)
        => mapper.Map<OrderDto>(await coreEntityService.GetByIdWithLockAsync(id, OrderLockOptions, cancellationToken));

    public async Task<OrderDto> CreateAsync(OrderDto order, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(order, nameof(order));
        
        return mapper.Map<OrderDto>(await coreEntityService.CreateAsync(mapper.Map<Entities.Order>(order), cancellationToken));
    }

    public async Task<OrderDto> UpdateAsync(OrderDto order, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(order, nameof(order));
        
        if (order.Id == Guid.Empty)
        {
            throw new ArgumentException("Order ID cannot be empty.", nameof(order));
        }
        
        var specification = new Specifications.OrderWithProductsSpecification();
        var userId = currentUserService.UserId;
        
        var updated = await coreEntityService.UpdateAsync(
            order.Id,
            existingOrder =>
            {
                mapper.Map(order, existingOrder);
                
                collectionSyncService.SyncChildCollection(
                    existingChildren: existingOrder.OrderProducts,
                    dtoChildren: order.OrderProducts,
                    getDtoKey: dto => dto.Id,
                    hasChanges: (entity, dto) => 
                        entity.ProductId != dto.ProductId ||
                        entity.Quantity != dto.Quantity ||
                        entity.Price != dto.Price ||
                        !entity.Concurrency.RowVersion.SequenceEqual(dto.RowVersion),
                    updateExisting: (entity, dto) =>
                    {
                        entity.ProductId = dto.ProductId;
                        entity.Quantity = dto.Quantity;
                        entity.Price = dto.Price;
                        entity.Concurrency.RowVersion = dto.RowVersion;
                    },
                    createNew: dto => new Entities.OrderProduct
                    {
                        OrderId = existingOrder.Id,
                        ProductId = dto.ProductId,
                        Quantity = dto.Quantity,
                        Price = dto.Price
                    },
                    childSoftDeleteService: orderProductSoftDeleteService,
                    childRepository: orderProductRepository,
                    userId: userId);
            },
            OrderLockOptions,
            specification,
            cancellationToken);
        
        return mapper.Map<OrderDto>(updated);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
        => await coreEntityService.DeleteAsync(id, OrderLockOptions, cancellationToken);
}
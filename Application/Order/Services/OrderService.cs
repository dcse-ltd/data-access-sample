using Application.Order.Dtos;
using Application.Order.Interfaces;
using AutoMapper;
using Infrastructure.Entity.Models;
using Infrastructure.Services.Interfaces;
using Infrastructure.Services.Models;

namespace Application.Order.Services;

public class OrderService(
    ICoreEntityService<Entities.Order> coreEntityService,
    IMapper mapper) : IOrderService
{
    private static readonly LockOptions OrderLockOptions = new()
    {
        IncludeChildren = true,
        MaxDepth = 1
    };
    
    public async Task<IEnumerable<OrderDto>> GetAllAsync(CancellationToken cancellationToken)
        => mapper.Map<IEnumerable<OrderDto>>(await coreEntityService.GetAllAsync(cancellationToken));

    public async Task<OrderDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => mapper.Map<OrderDto>(await coreEntityService.GetByIdAsync(id, cancellationToken));
    
    public async Task<OrderDto> GetByIdWithLockAsync(Guid id, CancellationToken cancellationToken = default)
        => mapper.Map<OrderDto>(await coreEntityService.GetByIdWithLockAsync(id, OrderLockOptions, cancellationToken));

    public async Task<OrderDto> CreateAsync(OrderDto order, CancellationToken cancellationToken)
        => mapper.Map<OrderDto>(await coreEntityService.CreateAsync(mapper.Map<Entities.Order>(order), cancellationToken));

    public async Task<OrderDto> UpdateAsync(OrderDto order, CancellationToken cancellationToken)
    {
        var updated = coreEntityService.UpdateAsync(
            order.Id,
            existing => mapper.Map(order, existing),
            OrderLockOptions,
            cancellationToken);
    
        return mapper.Map<OrderDto>(await updated);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
        => await coreEntityService.DeleteAsync(id, OrderLockOptions, cancellationToken);
}
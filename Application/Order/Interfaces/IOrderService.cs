using Application.Order.Dtos;

namespace Application.Order.Interfaces;

public interface IOrderService
{
    Task<IEnumerable<OrderDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<OrderDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<OrderDto> GetByIdWithLockAsync(Guid id, CancellationToken cancellationToken = default);
    Task<OrderDto> CreateAsync(OrderDto order, CancellationToken cancellationToken);
    Task<OrderDto> UpdateAsync(OrderDto order, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
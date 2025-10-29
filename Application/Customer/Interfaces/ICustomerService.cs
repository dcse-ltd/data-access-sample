using Application.Customer.Dtos;

namespace Application.Customer.Interfaces;

public interface ICustomerService
{
    Task<IEnumerable<CustomerDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<CustomerDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CustomerDto> GetByIdWithLockAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CustomerDto> CreateAsync(CustomerDto customer, CancellationToken cancellationToken);
    Task<CustomerDto> UpdateAsync(CustomerDto customer, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
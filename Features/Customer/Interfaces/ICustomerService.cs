using Features.Customer.Dtos;

namespace Features.Customer.Interfaces;

public interface ICustomerService
{
    Task<IEnumerable<CustomerDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<CustomerDto> GetByIdAsync(Guid id, bool lockForEdit = false, CancellationToken cancellationToken = default);
    Task<CustomerDto> CreateAsync(CustomerDto customer, CancellationToken cancellationToken);
    Task<CustomerDto> UpdateAsync(CustomerDto customer, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
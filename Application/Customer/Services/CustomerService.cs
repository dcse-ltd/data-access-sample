using Application.Customer.Dtos;
using Application.Customer.Interfaces;
using AutoMapper;
using Infrastructure.Services.Interfaces;

namespace Application.Customer.Services;

public class CustomerService(
    ICoreEntityService<Entities.Customer> coreEntityService,
    IMapper mapper) : ICustomerService
{
    public async Task<IEnumerable<CustomerDto>> GetAllAsync(CancellationToken cancellationToken)
        => mapper.Map<IEnumerable<CustomerDto>>(await coreEntityService.GetAllAsync(cancellationToken));

    public async Task<CustomerDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => mapper.Map<CustomerDto>(await coreEntityService.GetByIdAsync(id, cancellationToken));
    
    public async Task<CustomerDto> GetByIdWithLockAsync(Guid id, CancellationToken cancellationToken = default)
        => mapper.Map<CustomerDto>(await coreEntityService.GetByIdWithLockAsync(id, cancellationToken));

    public async Task<CustomerDto> CreateAsync(CustomerDto customer, CancellationToken cancellationToken)
        => mapper.Map<CustomerDto>(await coreEntityService.CreateAsync(mapper.Map<Entities.Customer>(customer), cancellationToken));

    public async Task<CustomerDto> UpdateAsync(CustomerDto customer, CancellationToken cancellationToken)
    {
        var updated = coreEntityService.UpdateAsync(
            customer.Id,
            existing => mapper.Map(customer, existing),
            cancellationToken);
    
        return mapper.Map<CustomerDto>(await updated);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
        => await coreEntityService.DeleteAsync(id, cancellationToken);
}
using Application.Customer.Dtos;
using Application.Customer.Interfaces;
using Application.Customer.Specifications;
using AutoMapper;
using Infrastructure.Repository.Models;
using Infrastructure.Services.Interfaces;

namespace Application.Customer.Services;

public class CustomerService(
    ICoreEntityService<Entities.Customer> coreEntityService,
    IMapper mapper) : ICustomerService
{
    public async Task<IEnumerable<CustomerDto>> GetAllAsync(
        CancellationToken cancellationToken)
        => mapper.Map<IEnumerable<CustomerDto>>(await coreEntityService.GetAllAsync(cancellationToken));

    public async Task<CustomerDto> GetByIdAsync(
        Guid id, 
        CancellationToken cancellationToken = default)
        => mapper.Map<CustomerDto>(await coreEntityService.GetByIdAsync(id, cancellationToken));
    
    public async Task<CustomerDto> GetByIdWithLockAsync(
        Guid id, 
        CancellationToken cancellationToken = default)
        => mapper.Map<CustomerDto>(await coreEntityService.GetByIdWithLockAsync(id, cancellationToken));

    public async Task<IEnumerable<CustomerDto>> FindCustomersAsync(
        string? lastName = null,
        string? emailAddress = null,
        string? phoneNumber = null,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var findCustomerSpecification = new FindCustomerSpecification(lastName, emailAddress, phoneNumber, page, pageSize);
        return mapper.Map<IEnumerable<CustomerDto>>(await coreEntityService.FindAsync(findCustomerSpecification, QueryOptions.Default, cancellationToken));
    }

    public async Task<CustomerDto> CreateAsync(
        CustomerDto customer, 
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(customer, nameof(customer));
        
        return mapper.Map<CustomerDto>(await coreEntityService.CreateAsync(mapper.Map<Entities.Customer>(customer), cancellationToken));
    }

    public async Task<CustomerDto> UpdateAsync(
        CustomerDto customer, 
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(customer, nameof(customer));
        
        if (customer.Id == Guid.Empty)
        {
            throw new ArgumentException("Customer ID cannot be empty.", nameof(customer));
        }
        
        var updated = await coreEntityService.UpdateAsync(
            customer.Id,
            existing => mapper.Map(customer, existing),
            cancellationToken);
    
        return mapper.Map<CustomerDto>(updated);
    }

    public async Task DeleteAsync(
        Guid id, 
        CancellationToken cancellationToken)
        => await coreEntityService.DeleteAsync(id, cancellationToken);
}
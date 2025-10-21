using AutoMapper;
using DataAccess.Exceptions;
using DataAccess.Repository.Interfaces;
using DataAccess.UnitOfWork.Interfaces;
using Features.Customer.Dtos;
using Features.Customer.Interfaces;
using Microsoft.Extensions.Logging;

namespace Features.Customer.Services;

public class CustomerService(
    IUnitOfWork unitOfWork,
    IRepository<Entities.Customer> customerRepository,
    IMapper mapper,
    ILogger logger) : ICustomerService
{
    public async Task<IEnumerable<CustomerDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        var customers = await customerRepository.GetAllAsync(cancellationToken);
        return customers.Select(mapper.Map<CustomerDto>);
    }

    public async Task<CustomerDto> GetByIdAsync(Guid id, bool lockForEdit = false, CancellationToken cancellationToken = default)
        => mapper.Map<CustomerDto>(await customerRepository.GetByIdAsync(id, false, cancellationToken));

    public async Task<CustomerDto> CreateAsync(CustomerDto customer, CancellationToken cancellationToken)
    {
        var newId = Guid.NewGuid();
        var newCustomer = mapper.Map<Entities.Customer>(customer);
        newCustomer.Id = newId;
        
        await customerRepository.AddAsync(newCustomer, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return mapper.Map<CustomerDto>(newCustomer);
    }

    public async Task<CustomerDto> UpdateAsync(CustomerDto customer, CancellationToken cancellationToken)
    {
        try
        {
            var existingCustomer = await customerRepository.GetByIdAsync(
                customer.Id, 
                true, 
                cancellationToken);
        
            if (existingCustomer == null)
                throw new EntityNotFoundException(nameof(Customer), customer.Id);
        
            mapper.Map(customer, existingCustomer);
        
            customerRepository.UpdateAsync(existingCustomer);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return mapper.Map<CustomerDto>(existingCustomer);
        }
        catch (ConcurrencyException e)
        {
            logger.LogWarning(
                "Concurrency conflict updating customer {CustomerId}: {Message}",
                customer.Id,
                e.Message);
            throw;
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var existingCustomer = await customerRepository.GetByIdAsync(id, true, cancellationToken);
        customerRepository.Remove(existingCustomer);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
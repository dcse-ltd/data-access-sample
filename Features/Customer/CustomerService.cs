using AutoMapper;
using DataAccess.Repository.Interfaces;
using DataAccess.Services.Interfaces;
using DataAccess.UnitOfWork.Interfaces;
using Features.Customer.Dtos;
using Features.Customer.Interfaces;

namespace Features.Customer;

public class CustomerService(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    IRepository<DataAccess.Entity.Customer> customerRepository,
    IMapper mapper) : ICustomerService
{
    public async Task<IEnumerable<CustomerDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        var customers = await customerRepository.GetAllAsync(cancellationToken);
        return customers.Select(mapper.Map<CustomerDto>);
    }

    public async Task<CustomerDto> GetByIdAsync(Guid id, bool lockForEdit = false, CancellationToken cancellationToken = default)
        => mapper.Map<CustomerDto>(await customerRepository.GetByIdAsync(id, lockForEdit, cancellationToken));

    public async Task<CustomerDto> CreateAsync(CustomerDto customer, CancellationToken cancellationToken)
    {
        var newId = Guid.NewGuid();
        var newCustomer = mapper.Map<DataAccess.Entity.Customer>(customer);
        newCustomer.Id = newId;
        
        await customerRepository.AddAsync(newCustomer, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var persistedCustomer = customerRepository.GetByIdAsync(newId, false, cancellationToken);
        return mapper.Map<CustomerDto>(persistedCustomer);
    }

    public async Task<CustomerDto> UpdateAsync(CustomerDto customer, CancellationToken cancellationToken)
    {
        var existingCustomer = await customerRepository.GetByIdAsync(customer.Id, false, cancellationToken);
        if (existingCustomer == null)
            throw new Exception("Customer not found");
        
        existingCustomer = mapper.Map<DataAccess.Entity.Customer>(customer);
        customerRepository.Update(existingCustomer);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var persistedCustomer = customerRepository.GetByIdAsync(customer.Id, false, cancellationToken);
        return mapper.Map<CustomerDto>(persistedCustomer);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var existingCustomer = await customerRepository.GetByIdAsync(id, false, cancellationToken);
        if (existingCustomer == null)
            throw new Exception("Customer not found");
        
        customerRepository.Remove(existingCustomer);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
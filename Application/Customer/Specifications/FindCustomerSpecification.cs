using Infrastructure.Repository.Specification;

namespace Application.Customer.Specifications;

public class FindCustomerSpecification : BaseSpecification<Entities.Customer>
{
    public FindCustomerSpecification(
        string? lastName = null, 
        string? emailAddress = null, 
        string? phoneNumber = null,
        int page = 1,
        int pageSize = 10)
    {
        if (lastName != null)
            AddCriteria(x => x.LastName.StartsWith(lastName));
        
        if (emailAddress != null)
            AddCriteria(x => x.Email.StartsWith(emailAddress));
        
        if (phoneNumber != null)
            AddCriteria(x => x.Phone.StartsWith(phoneNumber));
        
        AddInclude(x => x.Orders);
        
        ApplyOrderBy(x => x.LastName);
        
        ApplyPaging((page - 1) * pageSize, pageSize);
    }
}
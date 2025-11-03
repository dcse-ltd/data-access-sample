using Infrastructure.Repository.Specification;

namespace Application.Order.Specifications;

/// <summary>
/// Specification for retrieving an Order with its OrderProducts collection included.
/// </summary>
public class OrderWithProductsSpecification : BaseSpecification<Entities.Order>
{
    public OrderWithProductsSpecification()
    {
        AddInclude(order => order.OrderProducts);
        ApplyTracking(); // Enable tracking for updates
    }
}


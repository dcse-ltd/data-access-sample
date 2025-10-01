using Features.Customer;
using Features.Customer.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Features;

public static class ServiceComposition
{
    public static void RegisterFeatures(this IServiceCollection services)
    {
        services.AddScoped<ICustomerService, CustomerService>();
    }
}
using Features.Customer.Interfaces;
using Features.Customer.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Features;

public static class ServiceComposition
{
    public static void RegisterFeatures(this IServiceCollection services)
    {
        services.AddDbContext<AppDbContext>();
        services.AddScoped<ICustomerService, CustomerService>();
    }
}
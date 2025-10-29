using Application.Customer.Interfaces;
using Application.Customer.Services;
using Application.Order.Interfaces;
using Application.Order.Services;
using Application.User;
using Infrastructure.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class ServiceComposition
{
    public static void RegisterApplication(this IServiceCollection services)
    {
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IOrderService, OrderService>();
    }
}
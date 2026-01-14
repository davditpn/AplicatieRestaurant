using Microsoft.Extensions.DependencyInjection;
using AplicatieRestaurant.Application;

namespace AplicatieRestaurant.Application;

public static class ApplicationExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddTransient<RestaurantService>();
        return services;
    }
}
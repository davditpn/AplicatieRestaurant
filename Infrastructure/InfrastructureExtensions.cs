using Microsoft.Extensions.DependencyInjection;
using AplicatieRestaurant.Domain.Entities;
using AplicatieRestaurant.Domain.Interfaces;
using AplicatieRestaurant.Infrastructure.Repositories;



namespace AplicatieRestaurant.Infrastructure;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IRepository<Dish>>(new FileRepository<Dish>("dishes.json"));
        services.AddSingleton<IRepository<Order>>(new FileRepository<Order>("orders.json"));
        services.AddSingleton<IRepository<User>>(new FileRepository<User>("users.json"));
        services.AddSingleton<IRepository<Ingredient>>(new FileRepository<Ingredient>("ingredients.json"));
        services.AddSingleton<IRepository<RestaurantSettings>>(new FileRepository<RestaurantSettings>("settings.json"));
        return services;
    }
}
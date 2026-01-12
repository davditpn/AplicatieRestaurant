using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AplicatieRestaurant.Application;
using AplicatieRestaurant.Domain.Entities;
using AplicatieRestaurant.Domain.Enums;
using AplicatieRestaurant.Domain.Interfaces;
using AplicatieRestaurant.Infrastructure;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddInfrastructure();
        services.AddApplication();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
    })
    .Build();

using (var scope = host.Services.CreateScope())
{
    var service = scope.ServiceProvider.GetRequiredService<RestaurantService>();
    var userRepo = scope.ServiceProvider.GetRequiredService<IRepository<User>>();
}    
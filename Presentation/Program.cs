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

static void RunUI (RestaurantService service, IRepository<User> userRepo)
{
    while (true)
    {
        Console.WriteLine("\n=== APLICAȚIE RESTAURANT ===");
        Console.WriteLine("1. Client (Plasează comandă)");
        Console.WriteLine("2. Manager (Gestionează meniul și comenzile)");
        Console.WriteLine("0. Ieșire");
        Console.Write("Alege: ");
        var key= Console.ReadLine();

        if (key == "0") break;
        if (key == "1") RunClient(service, userRepo);
        if (key == "2") RunManager(service);
    }
}

static void SeedData(RestaurantService service, IRepository<User> userRepo)
{
    if (!service.GetMenu().Any())
    {
        Console.WriteLine("Seeding Menu...");
        service.AddDish("Pizza Margherita", 35,DishCategory.MainCourse);
        service.AddDish("Burger Vita", 45, DishCategory.MainCourse);
        service.AddDish("Papanasi",25,DishCategory.Dessert);
        service.AddDish("Limonada",15,DishCategory.Beverage);
    }

    if (!userRepo.GetAll().Any())
    {
        Console.WriteLine("Seeding Users...");
        userRepo.Add(new Client("client1","pass","Acasa"));
        userRepo.Add(new Manager("admin","admin"));
    }
}
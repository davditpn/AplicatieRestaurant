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
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
    })
    .Build();

using (var scope = host.Services.CreateScope())
{
    var service = scope.ServiceProvider.GetRequiredService<RestaurantService>();
    var userRepo = scope.ServiceProvider.GetRequiredService<IRepository<User>>();
    
    SeedData(service, userRepo);
    RunApp(service, userRepo);
}   

// static void RunUI (RestaurantService service, IRepository<User> userRepo)
// {
//     while (true)
//     {
//         Console.WriteLine("\n=== APLICAȚIE RESTAURANT ===");
//         Console.WriteLine("1. Client (Plasează comandă)");
//         Console.WriteLine("2. Manager (Gestionează meniul și comenzile)");
//         Console.WriteLine("0. Ieșire");
//         Console.Write("Alege: ");
//         var key= Console.ReadLine();
//
//         if (key == "0") break;
//         if (key == "1") RunClient(service, userRepo);
//         if (key == "2") RunManager(service);
//     }
// }

static void RunApp(RestaurantService service, IRepository<User> userRepo)
{
    while (true)
    {
        Console.Clear();
        Console.WriteLine("=== RESTAURANT APP - BINE ATI VENIT ===");
        Console.WriteLine("1. Authentification (Login)");
        Console.WriteLine("2. Register New Client (Sign Up)");
        Console.WriteLine("0. Exit");
        Console.WriteLine("Alege optiunea: ");
        
        var input  = Console.ReadLine();

        if (input == "0") break;
        if (input == "1") HandleLogin(service,  userRepo);
        if (input == "2") HandleRegister(service);
    }
}

static void HandleLogin(RestaurantService service, IRepository<User> userRepo)
{
    Console.WriteLine("\n--- LOGIN ---");
    Console.Write("Username: ");
    var user = Console.ReadLine();
    Console.Write("Password: ");
    var password = Console.ReadLine();
    
    var loggedUser = service.Login(user, password);

    if (loggedUser == null)
    {
        Console.WriteLine($"❌ {loggedUser.Username} nu exista sau parola este gresita! Apasa Enter!");
        Console.ReadLine();
        return;
    }
    
    Console.WriteLine($"✅ Autentificat ca: {loggedUser.Username} ({loggedUser.Role})");
    Thread.Sleep(1000);

    if (loggedUser is Manager manager)
    {
        RunManagerMenu(service, userRepo);
    }
    else if (loggedUser is Client client)
    {
        RunClientMenu(service, client);
    }
}

static void HandleRegister(RestaurantService service)
{
    Console.WriteLine("\n--- SIGN UP ---");
    Console.WriteLine("Username dorit: ");
    var user = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(user)) return;
    
    Console.Write("Parola: ");
    var password = Console.ReadLine();
    
    Console.WriteLine("Adresa de livrare: ");
    var address = Console.ReadLine();
    
    bool success = service.RegisterClient(user, password, address);

    if (success)
    {
        Console.WriteLine("✅ Cont creat! Te poti loga acum.");
    }
    else
    {
        Console.WriteLine("❌ Eroare: Username-ul exista deja!");
    }
    Console.WriteLine("\nApasa Enter!");
    Console.ReadLine();
}

// static void RunClient(RestaurantService service, IRepository<User> userRepo)
// {
//     var client = userRepo.GetAll().FirstOrDefault(u => u.Role == UserRole.Client);
//     if (client == null)
//     {
//         Console.Write("Nu exista niciun cont de client. Restartati aplicatia");
//         return;
//     }
//     
//     Console.WriteLine($"\n--- BINE AI VENIT, {client.Username}! ---");
//
//     var menu = service.GetMenu().ToList();
//     if (!menu.Any())
//     {
//         Console.Write("Meniul este gol.");
//         return;
//     }
//
//     var menuMap = new Dictionary<int, Dish>();
//     int index = 1;
//     Console.WriteLine("\n--- MENIU ---");
//     foreach (var dish in menu)
//     {
//         Console.WriteLine($"{index}. {dish.Name} - {dish.Price} RON");
//         menuMap[index] = dish;
//         index++;
//     }
//
//     var cart = new Dictionary<Guid, int>();
//
//     while (true)
//     {
//         Console.WriteLine("\nIntroduceti numarul produsului pentru a adauga (sau '0' pentru a finaliza):");
//         Console.WriteLine("> ");
//         var input = Console.ReadLine();
//
//         if (input == "0") break;
//
//         if (int.TryParse(input, out int selection) && menuMap.ContainsKey(selection))
//         {
//             var selectedDish = menuMap[selection];
//             
//             Console.Write($"Cantitate pentru {selectedDish.Name}: ");
//             if (int.TryParse(Console.ReadLine(), out int quantity) && quantity > 0)
//             {
//                 if((cart.ContainsKey(selectedDish.Id)))
//                         cart[selectedDish.Id] += quantity;
//                 else
//                     cart[selectedDish.Id] = quantity;
//                 
//                 Console.WriteLine($"-> Adaugat: {quantity} X {selectedDish.Name}");
//             }
//             else
//             {
//                 Console.WriteLine("Cantitate invalida.");
//             }
//         }
//         else
//         {
//             Console.WriteLine("Produs invalid.");
//         }
//     }
//
//     if (cart.Any())
//     {
//         try
//         {
//             var order = service.PlaceOrder(client.Id, cart);
//             Console.WriteLine("\n Comanda plasata cu succes!");
//             Console.WriteLine($"ID Comanda: {order.Id}");
//             Console.WriteLine($"Total de plata: {order.TotalPrice} RON");
//         }
//         catch (Exception ex)
//         {
//             Console.WriteLine($"Eroare la plasarea comenzii: {ex.Message}");
//         }
//     }
//     else
//     {
//         {
//             Console.WriteLine("Cosul este gol. Comanda anulata.");
//         }
//     }
// }

static void RunClientMenu(RestaurantService service, Client currentClient)
{
    var menu = service.GetMenu().ToList();
    var menuMap = new Dictionary<int, Dish>();
    int idx = 1;
    
    foreach (var d in menu)
    {
        menuMap[idx++] = d;
    }
    
    var cart = new Dictionary<Guid, int>();

    while (true)
    {
        Console.Clear();
        Console.WriteLine($"--- MENIU CLIENT: {currentClient.Username} ---");
        Console.WriteLine($"Adresa de livrare: {currentClient.DeliveryAddress}");
        Console.WriteLine("------------------------------");

        foreach (var kvp in menuMap)
        {
            Console.WriteLine($"{kvp.Key}. {kvp.Value.Name} - {kvp.Value.Price} RON");
        }
        Console.WriteLine("0. Finalizeaza Comanda / Logout");
        
        if(cart.Any()) Console.WriteLine($"\n[Cos]: {cart.Count} produse.");
        
        Console.Write("\nAlege produs (nr): ");
        var input = Console.ReadLine();

        if (input == "0")
        {
            if (cart.Any())
            {
                service.PlaceOrder(currentClient.Id, cart);
                Console.WriteLine("Comanda trimisa! (Enter)");
                Console.ReadLine();
            }

            break;
        }

        if (int.TryParse(input, out int selection) && menuMap.ContainsKey(selection))
        {
            var dish = menuMap[selection];
            Console.WriteLine($"Cantitate pentru {dish.Name}: ");
            if (int.TryParse(Console.ReadLine(), out int quantity) && quantity > 0)
            {
                if (!cart.ContainsKey(dish.Id))
                {
                    cart[dish.Id] = 0;
                }
                cart[dish.Id] += quantity;
            }
        }
    }
}

// static void RunManager(RestaurantService service)
// {
//     while (true)
//     {
//         Console.Clear();
//         Console.WriteLine("--- PANOU MANAGER ---");
//         var orders=service.GetAllOrders().OrderByDescending(o=>o.CreatedAt).ToList();
//         if (!orders.Any())
//         {
//             Console.WriteLine("Nu exista comenzi active.");
//         }
//         else
//         {
//             foreach (var ord in orders)
//             {
//                 Console.WriteLine($"ID: {ord.Id}");
//                 Console.WriteLine($"Data: {ord.CreatedAt.ToLocalTime()} | Status: {ord.Status} | Total: {ord.TotalPrice} RON");
//                 Console.WriteLine("Produse:");
//                 foreach (var item in ord.Items)
//                 {
//                     Console.WriteLine($"   - {item.Quantity}x {item.DishName}");
//                 }
//                 Console.WriteLine(new string('-',30));
//             }
//         }
//         Console.WriteLine("\nActiuni:");
//         Console.WriteLine("1. Modifica Status Comanda");
//         Console.WriteLine("0. Inapoi la Meniul Principal");
//         Console.Write("> ");
//         var choice = Console.ReadLine();
//         if(choice=="0") break;
//         if (choice == "1")
//         {
//             Console.WriteLine("Introduceti ID-ul comenzii:");
//             var idStr = Console.ReadLine();
//             if (Guid.TryParse(idStr, out Guid orderId))
//             {
//                 Console.WriteLine("Status: 1.Preparing, 2.Ready, 3.Completed, 4.Canceled");
//                 var sInput = Console.ReadLine();
//                 OrderStatus? newStatus = sInput switch
//                 {
//                     "1" => OrderStatus.Preparing,
//                     "2" => OrderStatus.Ready,
//                     "3" => OrderStatus.Completed,
//                     "4" => OrderStatus.Canceled,
//                     _ => null
//                 };
//                 if(newStatus.HasValue)
//                     service.UpdateOrderStatus(orderId, newStatus.Value);
//             }
//         }
//
//         if (choice == "2")
//         {
//             Console.WriteLine("Introduceti ID-ul comenzii de sters:");
//             var idStr=Console.ReadLine();
//             if (Guid.TryParse(idStr, out Guid orderId))
//             {
//                 Console.Write("Sigur doriti sa stergeti? (da/nu): ");
//                 if (Console.ReadLine()?.ToLower() == "da")
//                 {
//                     service.DeleteOrder(orderId);
//                     Console.WriteLine("Comanda a fost stearsa.");
//                     Console.ReadLine();
//                 }
//             }
//         }
//     }
// }

static void RunManagerMenu(RestaurantService service, IRepository<User> userRepo)
{
    while (true)
    {
        Console.Clear();
        Console.WriteLine("--- MENiU MANAGER ---");
        
        var orders = service.GetAllOrders().OrderByDescending(o => o.CreatedAt).ToList();
        
        if(!orders.Any()) Console.WriteLine("Nu exista comenzi");
        else
        {
            foreach (var order in orders)
            {
                var clientName = "Necunoscut";
                var clientUser = userRepo.GetById(order.ClientId);
                if (clientUser != null)
                {
                    clientName = clientUser.Username;
                }
                
                Console.WriteLine($"ORDER: {order.Id}");
                Console.WriteLine($"CLIENT: {clientName} (ID: {order.ClientId})");
                Console.WriteLine($"Data: {order.CreatedAt.ToLocalTime()} | Status: {order.Status} | Total: {order.TotalPrice} RON");

                foreach (var item in order.Items)
                {
                    Console.WriteLine($"   -{item.Quantity} x {item.DishName}");
                }
                
                Console.WriteLine(new string('-', 40));
            }
        }
        
        Console.WriteLine("\n1. Modifica Status | 2. Sterge Comanda | 0. Logout");
        Console.WriteLine("> ");
        var choice = Console.ReadLine();

        if (choice == "0") break;

        if (choice == "1")
        {
            Console.Write("ID Comanda: ");
            if (Guid.TryParse(Console.ReadLine(), out Guid oid))
            {
                Console.Write("Status (1.Prep, 2.Ready, 3.Done, 4.Cancel): ");
                var s =  Console.ReadLine();
                OrderStatus? status = s switch
                {
                    "1" => OrderStatus.Preparing, 
                    "2" => OrderStatus.Ready, 
                    "3" => OrderStatus.Completed,
                    "4" => OrderStatus.Canceled,
                    _ => null
                };
                if(status.HasValue) service.UpdateOrderStatus(oid, status.Value);
            }
        }

        if (choice == "2")
        {
            Console.Write("ID Comanda: ");
            if (Guid.TryParse(Console.ReadLine(), out Guid oid)) service.DeleteOrder(oid);
        }
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
        // Manager Default
        userRepo.Add(new Manager("admin", "admin")); 
        // Client de test
        userRepo.Add(new Client("client1", "pass", "Strada Libertatii 1"));
    }
}
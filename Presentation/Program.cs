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
        Console.WriteLine($"❌ {user} nu exista sau parola este gresita! Apasa Enter!");
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

static void HandleMenuManagement(RestaurantService service)
{
    while (true)
    {
        Console.Clear();
        Console.WriteLine("--- GESTIONARE MENIU ---");
        
        var menu = service.GetMenu().OrderBy(d => d.Category).ToList();
        
        Console.WriteLine(string.Format("{0,-5} | {1,-20} | {2,-10} | {3}", "ID", "Nume", "Pret", "Categorie"));
        Console.WriteLine(new string('-', 60));
        
        var localMap = new Dictionary<int, Dish>();
        int idx = 1;

        foreach (var dish in menu)
        {
            string catRo = dish.Category switch {
                DishCategory.Appetizer => "Aperitiv",
                DishCategory.MainCourse => "Fel P.",
                DishCategory.Dessert => "Desert",
                DishCategory.Beverage => "Bautura",
                _ => dish.Category.ToString()
            };

            Console.WriteLine($"{idx,-5} | {dish.Name,-20} | {dish.Price,-10} | {catRo}");
            localMap[idx] = dish;
            idx++;
        }

        Console.WriteLine("\nOptiuni:");
        Console.WriteLine("A. Adauga Produs Nou");
        Console.WriteLine("M. Modifica Produs");
        Console.WriteLine("S. Sterge Produs");
        Console.WriteLine("0. Inapoi");
        Console.Write("> ");
        
        var input = Console.ReadLine()?.ToUpper();

        if (input == "0") break;
        
        if (input == "A")
        {
            Console.Write("Nume produs: ");
            var name = Console.ReadLine();
            Console.Write("Pret: ");
            decimal.TryParse(Console.ReadLine(), out decimal price);
            
            Console.WriteLine("Categorie: 1.Aperitiv, 2.Fel Principal, 3.Desert, 4.Bautura");
            var catInput = Console.ReadLine();
            DishCategory cat = catInput switch {
                "1" => DishCategory.Appetizer,
                "2" => DishCategory.MainCourse,
                "3" => DishCategory.Dessert,
                "4" => DishCategory.Beverage,
                _ => DishCategory.MainCourse
            };

            service.AddDish(name, price, cat);
            Console.WriteLine("Produs adaugat! Enter.");
            Console.ReadLine();
        }
        
        if (input == "S")
        {
            Console.Write("Introduce numarul produsului (din lista de sus): ");
            if (int.TryParse(Console.ReadLine(), out int sel) && localMap.ContainsKey(sel))
            {
                service.RemoveDish(localMap[sel].Id);
                Console.WriteLine("Produs sters! Enter.");
                Console.ReadLine();
            }
        }
        
        if (input == "M")
        {
            Console.Write("Numar produs de modificat: ");
            if (int.TryParse(Console.ReadLine(), out int sel) && localMap.ContainsKey(sel))
            {
                var oldDish = localMap[sel];
                
                Console.WriteLine($"\nModifici: {oldDish.Name}");
                Console.WriteLine("(Lasa gol si apasa Enter pentru a pastra valoarea veche)");

                Console.Write($"Nume nou [{oldDish.Name}]: ");
                var newName = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(newName)) newName = oldDish.Name;

                Console.Write($"Pret nou [{oldDish.Price}]: ");
                var priceInput = Console.ReadLine();
                decimal newPrice = oldDish.Price;
                if (!string.IsNullOrWhiteSpace(priceInput)) decimal.TryParse(priceInput, out newPrice);

                Console.WriteLine($"Categorie curenta: {oldDish.Category}");
                Console.WriteLine("Categorie noua (1.Ap, 2.Main, 3.Des, 4.Baut) sau Enter pt neschimbat:");
                var catInput = Console.ReadLine();
                DishCategory newCat = oldDish.Category;
                if (!string.IsNullOrWhiteSpace(catInput))
                {
                     newCat = catInput switch {
                        "1" => DishCategory.Appetizer,
                        "2" => DishCategory.MainCourse,
                        "3" => DishCategory.Dessert,
                        "4" => DishCategory.Beverage,
                        _ => oldDish.Category
                    };
                }

                service.UpdateDish(oldDish.Id, newName, newPrice, newCat);
                Console.WriteLine("Produs actualizat! Enter.");
                Console.ReadLine();
            }
        }
    }
}

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

static void RunManagerMenu(RestaurantService service, IRepository<User> userRepo)
{
    while (true)
    {
        Console.Clear();
        Console.WriteLine("--- MENIU MANAGER ---");
        
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
        
        Console.WriteLine("\nACTIUNI:");
        Console.WriteLine("1. Modifica Status Comanda");
        Console.WriteLine("2. Sterge Comanda");
        Console.WriteLine("3. GESTIONARE MENIU (Adauga/Modifica/Sterge Produse)");
        Console.WriteLine("0. Logout");
        Console.Write("> ");
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
        
        if (choice == "3") HandleMenuManagement(service);
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
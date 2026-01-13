using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AplicatieRestaurant.Application;
using AplicatieRestaurant.Domain.Entities;
using AplicatieRestaurant.Domain.Enums;
using AplicatieRestaurant.Domain.Interfaces;
using AplicatieRestaurant.Infrastructure;
using AplicatieRestaurant.Infrastructure.Repositories;
using AplicatieRestaurant.Application;
using AplicatieRestaurant.Domain.Entities;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddInfrastructure();
        services.AddSingleton<IRepository<Ingredient>>(provider => new FileRepository<Ingredient>("ingredients.json"));
        services.AddApplication();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
    })
    .Build();

using (var scope = host.Services.CreateScope())
{
    var service = scope.ServiceProvider.GetRequiredService<RestaurantService>();
    var userRepo = scope.ServiceProvider.GetRequiredService<IRepository<User>>();
    var ingRepo = scope.ServiceProvider.GetRequiredService<IRepository<Ingredient>>();

    SeedData(service, userRepo, ingRepo);
    RunApp(service, userRepo);
}

static void RunApp(RestaurantService service, IRepository<User> userRepo)
{
    while (true)
    {
        Console.Clear();
        Console.WriteLine("=== RESTAURANT APP (Cu Stocuri) ===");
        Console.WriteLine("1. Login");
        Console.WriteLine("2. Sign Up");
        Console.WriteLine("0. Exit");
        Console.Write("> ");
        var input = Console.ReadLine();

        if (input == "0") break;
        if (input == "1") HandleLogin(service, userRepo);
        if (input == "2") HandleRegister(service);
    }
}

static void HandleLogin(RestaurantService service, IRepository<User> userRepo)
{
    Console.Write("User: "); var u = Console.ReadLine();
    Console.Write("Pass: "); var p = Console.ReadLine();
    var user = service.Login(u, p);

    if (user == null) { Console.WriteLine("Login failed!"); Console.ReadLine(); return; }

    if (user is Manager) RunManagerMenu(service, userRepo);
    else if (user is Client c) RunClientMenu(service, c);
}

static void HandleRegister(RestaurantService service)
{
    Console.Write("User nou: "); var u = Console.ReadLine();
    Console.Write("Pass: "); var p = Console.ReadLine();
    Console.Write("Adresa: "); var a = Console.ReadLine();
    if (service.RegisterClient(u, p, a)) Console.WriteLine("Success! Enter.");
    else Console.WriteLine("User existent.");
    Console.ReadLine();
}

static void RunClientMenu(RestaurantService service, Client client)
{
    var cart = new Dictionary<Guid, int>();
    
    while (true)
    {
        Console.Clear();
        Console.WriteLine($"Client: {client.Username} | Coș: {cart.Count} produse");
        Console.WriteLine("--- MENIU ---");

        var menu = service.GetMenu().ToList();
        var menuMap = new Dictionary<int, Dish>();
        int idx = 1;

        foreach (var dish in menu)
        {
            bool available = service.IsDishAvailable(dish);
            string status = available ? "" : " (INDISPONIBIL - Lipsa ingrediente)";
            
            Console.ForegroundColor = available ? ConsoleColor.Green : ConsoleColor.Red;
            Console.WriteLine($"{idx}. {dish.Name} - {dish.Price} RON {status}");
            Console.ResetColor();
            
            menuMap[idx++] = dish;
        }

        Console.WriteLine("0. Comandă / Iesi");
        Console.Write("> ");
        var input = Console.ReadLine();

        if (input == "0")
        {
            if (cart.Any())
            {
                try {
                    service.PlaceOrder(client.Id, cart);
                    Console.WriteLine("✅ Comanda plasata! Stocul a fost scazut.");
                } catch (Exception ex) {
                    Console.WriteLine($"❌ EROARE: {ex.Message}");
                }
                Console.ReadLine();
            }
            break;
        }

        if (int.TryParse(input, out int sel) && menuMap.ContainsKey(sel))
        {
            var dish = menuMap[sel];
            if (!service.IsDishAvailable(dish))
            {
                Console.WriteLine("Acest produs nu este disponibil!");
                Console.ReadLine();
                continue;
            }

            Console.Write("Cantitate: ");
            if (int.TryParse(Console.ReadLine(), out int q) && q > 0)
            {
                if (!cart.ContainsKey(dish.Id)) cart[dish.Id] = 0;
                cart[dish.Id] += q;
            }
        }
    }
}

static void RunManagerMenu(RestaurantService service, IRepository<User> userRepo)
{
    while (true)
    {
        Console.Clear();
        Console.WriteLine("--- MANAGER ---");
        Console.WriteLine("1. Vezi Comenzi");
        Console.WriteLine("2. GESTIONARE MENIU (Preparate)");
        Console.WriteLine("3. GESTIONARE STOCURI (Ingrediente)");
        Console.WriteLine("0. Logout");
        Console.Write("> ");
        var ch = Console.ReadLine();

        if (ch == "0") break;
        if (ch == "1") ShowOrders(service, userRepo);
        if (ch == "2") HandleMenuMgmt(service);
        if (ch == "3") HandleStockMgmt(service);
    }
}

static void HandleStockMgmt(RestaurantService service)
{
    while (true)
    {
        Console.Clear();
        Console.WriteLine("--- DEPOZIT INGREDIENTE ---");
        var stock = service.GetInventory().ToList();
        foreach (var item in stock)
        {
            Console.WriteLine($"- {item.Name}: {item.StockQuantity} {item.Unit}");
        }

        Console.WriteLine("\n1. Adauga Ingredient Nou");
        Console.WriteLine("2. Actualizeaza Stoc (Restock)");
        Console.WriteLine("0. Inapoi");
        
        var k = Console.ReadLine();
        if (k == "0") break;

        if (k == "1")
        {
            Console.Write("Nume: "); var n = Console.ReadLine();
            Console.Write("Unitate (kg/buc): "); var u = Console.ReadLine();
            Console.Write("Stoc Initial: "); double.TryParse(Console.ReadLine(), out double q);
            service.AddIngredientToStock(n, u, q);
        }
        if (k == "2")
        {
            Console.WriteLine("Scrie numele exact al ingredientului:");
            var name = Console.ReadLine();
            var item = stock.FirstOrDefault(i => i.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (item != null)
            {
                Console.Write("Noul stoc total: ");
                if (double.TryParse(Console.ReadLine(), out double q))
                    service.UpdateStock(item.Id, q);
            }
            else Console.WriteLine("Nu a fost găsit.");
        }
    }
}

static void HandleMenuMgmt(RestaurantService service)
{
    while (true)
    {
        Console.Clear();
        Console.WriteLine("=== GESTIONARE MENIU (Manager) ===");
        
        var menu = service.GetMenu().OrderBy(d => d.Category).ToList();
        
        var menuMap = new Dictionary<int, Dish>();
        int index = 1;

        if (!menu.Any())
        {
            Console.WriteLine("(!) Meniul este gol momentan.");
        }
        else
        {
            Console.WriteLine(string.Format("{0,-4} | {1,-20} | {2,-8} | {3,-10} | {4}", "Nr", "Nume", "Pret", "Categorie", "Stare Stoc"));
            Console.WriteLine(new string('-', 70));

            foreach (var dish in menu)
            {
                bool isAvailable = service.IsDishAvailable(dish);
                string stockStatus = isAvailable ? "OK" : "Lipsa Ingred.";

                Console.WriteLine(string.Format("{0,-4} | {1,-20} | {2,-8} | {3,-10} | {4}", 
                    index, 
                    dish.Name, 
                    dish.Price + " Lei", 
                    dish.Category,
                    stockStatus));
                
                menuMap[index] = dish;
                index++;
            }
        }
        Console.WriteLine(new string('-', 70));
        
        Console.WriteLine("\nACTIUNI:");
        Console.WriteLine("[A] - ADAUGA preparat nou");
        Console.WriteLine("[S] - STERGE un preparat");
        Console.WriteLine("[0] - Inapoi la meniul principal");
        Console.Write("> ");
        
        var input = Console.ReadLine()?.ToUpper();

        if (input == "0") break;
        
        if (input == "A")
        {
            Console.WriteLine("\n--- ADAUGARE PREPARAT ---");
            Console.Write("Nume Produs: "); var name = Console.ReadLine();
            Console.Write("Pret: "); decimal.TryParse(Console.ReadLine(), out decimal price);
            
            Console.WriteLine("Categorie: 1.MainCourse, 2.Appetizer, 3.Dessert, 4.Beverage");
            var catInput = Console.ReadLine();
            DishCategory cat = catInput switch {
                "2" => DishCategory.Appetizer,
                "3" => DishCategory.Dessert,
                "4" => DishCategory.Beverage,
                _ => DishCategory.MainCourse
            };
            
            var recipe = new List<RecipeItem>();
            var inventory = service.GetInventory().ToList();

            if (!inventory.Any())
            {
                Console.WriteLine("⚠️ ATENTIE: Nu ai ingrediente definite in stoc! Preparatul va fi creat fara reteta.");
            }
            else
            {
                while (true)
                {
                    Console.WriteLine("\n-- Adauga ingrediente la reteta --");
                    for(int i=0; i<inventory.Count; i++) 
                        Console.WriteLine($"{i+1}. {inventory[i].Name} ({inventory[i].Unit})");
                    
                    Console.Write("Alege nr ingredient (sau 0 pt a termina): ");
                    if (int.TryParse(Console.ReadLine(), out int idx))
                    {
                        if (idx == 0) break;
                        if (idx > 0 && idx <= inventory.Count)
                        {
                            var ing = inventory[idx-1];
                            Console.Write($"Cantitate necesara ({ing.Unit}): ");
                            if (double.TryParse(Console.ReadLine(), out double qty))
                            {
                                recipe.Add(new RecipeItem { IngredientId = ing.Id, IngredientName = ing.Name, QuantityRequired = qty });
                            }
                        }
                    }
                }
            }

            service.AddDish(name, price, cat, recipe);
            Console.WriteLine("✅ Produs adăugat! Apasă Enter.");
            Console.ReadLine();
        }
        
        if (input == "S")
        {
            Console.Write("\nIntroduce NUMARUL produsului de șters (din lista de sus): ");
            if (int.TryParse(Console.ReadLine(), out int selection) && menuMap.ContainsKey(selection))
            {
                var dishToDelete = menuMap[selection];
                
                Console.Write($"Sigur stergi '{dishToDelete.Name}'? (da/nu): ");
                if (Console.ReadLine()?.ToLower() == "da")
                {
                    service.RemoveDish(dishToDelete.Id);
                    Console.WriteLine("Produs sters.");
                }
                else
                {
                    Console.WriteLine("Anulat.");
                }
            }
            else
            {
                Console.WriteLine("Numar invalid!");
            }
            Console.ReadLine();
        }
    }
}

static void ShowOrders(RestaurantService service, IRepository<User> uRepo)
{
    foreach(var o in service.GetAllOrders())
        Console.WriteLine($"Order {o.Id} - Status: {o.Status}");
    Console.ReadLine();
}

static void SeedData(RestaurantService s, IRepository<User> u, IRepository<Ingredient> i)
{
    if (!u.GetAll().Any()) u.Add(new Manager("admin", "admin"));
    
    if (!i.GetAll().Any())
    {
        s.AddIngredientToStock("Faina", "kg", 10);
        s.AddIngredientToStock("Mozzarella", "kg", 5);
        s.AddIngredientToStock("Sos Rosii", "l", 5);
        s.AddIngredientToStock("Apa", "l", 50);
        s.AddIngredientToStock("Lamai", "buc", 20);
    }
    
    if (!s.GetMenu().Any())
    {
        var faina = i.GetAll().First(x => x.Name == "Faina");
        var mozza = i.GetAll().First(x => x.Name == "Mozzarella");
        
        var pizzaRecipe = new List<RecipeItem> 
        { 
            new RecipeItem { IngredientId = faina.Id, IngredientName = "Faina", QuantityRequired = 0.2 },
            new RecipeItem { IngredientId = mozza.Id, IngredientName = "Mozzarella", QuantityRequired = 0.1 }
        };
        
        s.AddDish("Pizza", 30, DishCategory.MainCourse, pizzaRecipe);
    }
}
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AplicatieRestaurant.Application;
using AplicatieRestaurant.Domain.Entities;
using AplicatieRestaurant.Domain.Enums;
using AplicatieRestaurant.Domain.Interfaces;
using AplicatieRestaurant.Infrastructure;
using AplicatieRestaurant.Infrastructure.Repositories;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddInfrastructure();
        
        services.AddSingleton<IRepository<Ingredient>>(p => new FileRepository<Ingredient>("ingredients.json"));
        services.AddSingleton<IRepository<RestaurantSettings>>(p => new FileRepository<RestaurantSettings>("settings.json"));
        
        services.AddApplication();
        
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
    })
    .Build();

using (var scope = host.Services.CreateScope())
{
    var service = scope.ServiceProvider.GetRequiredService<RestaurantService>();
    var userRepo = scope.ServiceProvider.GetRequiredService<IRepository<User>>();
    var ingRepo = scope.ServiceProvider.GetRequiredService<IRepository<Ingredient>>();
    var setRepo = scope.ServiceProvider.GetRequiredService<IRepository<RestaurantSettings>>();
    
    SeedData(service, userRepo, ingRepo, setRepo);
    
    RunApp(service, userRepo);
}


static void RunApp(RestaurantService service, IRepository<User> userRepo)
{
    while (true)
    {
        Console.Clear();
        Console.WriteLine("=== RESTAURANT APP ===");
        Console.WriteLine("1. Autentificare (Login)");
        Console.WriteLine("2. Inregistrare Client (Sign Up)");
        Console.WriteLine("0. Iesire");
        Console.Write("Alege: ");
        
        var input = Console.ReadLine();

        if (input == "0") break;
        if (input == "1") HandleLogin(service, userRepo);
        if (input == "2") HandleRegister(service);
    }
}

static void HandleLogin(RestaurantService service, IRepository<User> userRepo)
{
    Console.WriteLine("\n--- LOGIN ---");
    Console.Write("Username: "); var user = Console.ReadLine();
    Console.Write("Password: "); var pass = Console.ReadLine();
    
    var loggedUser = service.Login(user, pass);

    if (loggedUser == null)
    {
        Console.WriteLine($"User-ul '{user}' nu exista sau parola este gresita! (Enter)");
        Console.ReadLine();
        return;
    }
    
    Console.WriteLine($"Bine ai venit, {loggedUser.Username}!");
    Thread.Sleep(800);

    if (loggedUser is Manager)
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
    Console.Write("Username dorit: "); var user = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(user)) return;
    
    Console.Write("Parola: "); var pass = Console.ReadLine();
    Console.Write("Adresa Livrare: "); var addr = Console.ReadLine();
    
    bool success = service.RegisterClient(user, pass, addr);

    if (success) Console.WriteLine("Cont creat! Te poti loga acum.");
    else Console.WriteLine("Username-ul exista deja!");
    
    Console.ReadLine();
}


static void RunClientMenu(RestaurantService service, Client client)
{
    var cart = new List<(Guid DishId, int Qty, string Note)>(); 
    
    while (true)
    {
        Console.Clear();
        Console.WriteLine($"--- CLIENT: {client.Username} ---");
        Console.WriteLine($"Cos: {cart.Sum(x => x.Qty)} produse");
        Console.WriteLine("-----------------------------");
        Console.WriteLine("1. Vezi MENIU & Comanda");
        Console.WriteLine("2. Istoric Comenzi & Status");
        Console.WriteLine("0. Logout");
        Console.Write("> ");
        
        var mainChoice = Console.ReadLine();
        if (mainChoice == "0") break;

        if (mainChoice == "2")
        {
            ShowClientHistory(service, client.Id);
            continue;
        }

        if (mainChoice == "1")
        {
            HandleClientOrder(service, client, cart);
        }
    }
}

static void ShowClientHistory(RestaurantService service, Guid clientId)
{
    Console.Clear();
    Console.WriteLine("--- ISTORICUL MEU ---");
    var history = service.GetClientHistory(clientId).ToList();

    if (!history.Any()) Console.WriteLine("Nu ai nicio comanda anterioara.");
    else
    {
        foreach (var ord in history)
        {
            var tip = ord.IsDelivery ? "Livrare" : "Ridicare";
            Console.WriteLine($"[{ord.CreatedAt.ToLocalTime():g}] | {tip} | Status: {ord.Status}");
            Console.WriteLine($"Total: {ord.TotalPrice} RON (Taxa livrare: {ord.DeliveryFee})");
            
            foreach(var item in ord.Items)
            {
                var note = string.IsNullOrEmpty(item.SpecialNote) ? "" : $" (Note: {item.SpecialNote})";
                Console.WriteLine($"   - {item.Quantity} x {item.DishName}{note}");
            }
            Console.WriteLine(new string('-', 40));
        }
    }
    Console.WriteLine("Apasa Enter pentru a reveni.");
    Console.ReadLine();
}

static void HandleClientOrder(RestaurantService service, Client client, List<(Guid DishId, int Qty, string Note)> cart)
{
    var menu = service.GetMenu().ToList();
    var menuMap = new Dictionary<int, Dish>();
    int idx = 1;

    while (true)
    {
        Console.Clear();
        Console.WriteLine("--- PLASARE COMANDA ---");
        
        foreach (var dish in menu)
        {
            bool avail = service.IsDishAvailable(dish);
            var status = avail ? "" : " (INDISPONIBIL)";
            
            Console.ForegroundColor = avail ? ConsoleColor.Green : ConsoleColor.Red;
            Console.WriteLine($"{idx}. {dish.Name} - {dish.Price} RON {status}");
            Console.ResetColor();
            
            menuMap[idx++] = dish;
        }
        
        if(cart.Any())
        {
            Console.WriteLine("\n[CONTINUT COS]:");
            foreach(var c in cart) 
            {
                var dName = menu.FirstOrDefault(d => d.Id == c.DishId)?.Name;
                var noteDisplay = string.IsNullOrEmpty(c.Note) ? "" : $" [Obs: {c.Note}]";
                Console.WriteLine($" • {c.Qty} x {dName}{noteDisplay}");
            }
        }

        Console.WriteLine("\nTASTE: Numar produs | 'FIN' pt finalizare | '0' Inapoi");
        Console.Write("> ");
        var input = Console.ReadLine()?.ToUpper();

        if (input == "0") break;
        
        if (input == "FIN")
        {
            if (!cart.Any()) { Console.WriteLine("Cosul este gol."); Console.ReadLine(); continue; }
            
            var settings = service.GetSettings();
            Console.WriteLine($"\nCum doresti comanda?");
            Console.WriteLine($"1. Ridicare Personala (0 RON)");
            Console.WriteLine($"2. Livrare la domiciliu (+{settings.DeliveryCost} RON, ~{settings.DeliveryTimeMinutes} min)");
            Console.Write("Alege (1 sau 2): ");
            var delChoice = Console.ReadLine();
            bool isDelivery = delChoice == "2";

            try 
            {
                var order = service.PlaceOrder(client.Id, cart, isDelivery);
                Console.WriteLine($"\nComanda plasata cu succes!");
                Console.WriteLine($"Total de plata: {order.TotalPrice} RON");
                if(isDelivery) Console.WriteLine($"Livrare la: {client.DeliveryAddress}");
                
                cart.Clear();
                Console.ReadLine();
                break;
            } 
            catch (Exception ex) 
            {
                Console.WriteLine($"EROARE: {ex.Message}");
                Console.ReadLine();
            }
            continue;
        }
        
        if (int.TryParse(input, out int sel) && menuMap.ContainsKey(sel))
        {
            var dish = menuMap[sel];
            
            if (!service.IsDishAvailable(dish)) 
            { 
                Console.WriteLine("Produs indisponibil (lipsa ingrediente)."); 
                Console.ReadLine(); 
                continue; 
            }

            Console.Write($"Cantitate pt {dish.Name}: ");
            if (int.TryParse(Console.ReadLine(), out int q) && q > 0)
            {
                Console.Write("Observatii speciale sau Enter: ");
                var note = Console.ReadLine() ?? "";
                
                cart.Add((dish.Id, q, note));
            }
        }
        idx = 1;
    }
}

static void RunManagerMenu(RestaurantService service, IRepository<User> userRepo)
{
    while (true)
    {
        Console.Clear();
        Console.WriteLine("--- PANOU MANAGER ---");
        Console.WriteLine("1. GESTIONARE COMENZI (Status)");
        Console.WriteLine("2. GESTIONARE MENIU (Preparate)");
        Console.WriteLine("3. GESTIONARE STOCURI (Ingrediente)");
        Console.WriteLine("4. CONFIGURARE LIVRARE");
        Console.WriteLine("0. Logout");
        Console.Write("> ");
        var ch = Console.ReadLine();

        if (ch == "0") break;
        if (ch == "1") HandleOrderManagement(service, userRepo);
        if (ch == "2") HandleMenuMgmt(service);
        if (ch == "3") HandleStockMgmt(service);
        if (ch == "4") HandleDeliveryConfig(service);
    }
}

static void HandleOrderManagement(RestaurantService service, IRepository<User> userRepo)
{
    while (true)
    {
        Console.Clear();
        Console.WriteLine("=== COMENZI ACTIVE ===");
        
        var allOrders = service.GetAllOrders();
        var sortedOrders = allOrders
            .OrderBy(o => o.Status == OrderStatus.Completed || o.Status == OrderStatus.Canceled)
            .ThenByDescending(o => o.CreatedAt)
            .ToList();

        var orderMap = new Dictionary<int, Order>();
        int index = 1;

        if (!sortedOrders.Any()) Console.WriteLine("Nu exista comenzi.");
        else
        {
            foreach (var order in sortedOrders)
            {
                var clientName = userRepo.GetById(order.ClientId)?.Username ?? "Necunoscut";
                var tipLivrare = order.IsDelivery ? "LIVRARE" : "RIDICARE";

                Console.Write($"[{index}] {order.CreatedAt.ToLocalTime():HH:mm} | {clientName} | {tipLivrare} | Status: ");
                
                switch (order.Status)
                {
                    case OrderStatus.Created: Console.ForegroundColor = ConsoleColor.Yellow; break;
                    case OrderStatus.Preparing: Console.ForegroundColor = ConsoleColor.Cyan; break;
                    case OrderStatus.Ready: Console.ForegroundColor = ConsoleColor.Green; break;
                    case OrderStatus.Completed: Console.ForegroundColor = ConsoleColor.DarkGray; break;
                    case OrderStatus.Canceled: Console.ForegroundColor = ConsoleColor.Red; break;
                }
                Console.WriteLine(order.Status);
                Console.ResetColor();

                foreach (var item in order.Items)
                {
                    var note = string.IsNullOrEmpty(item.SpecialNote) ? "" : $" [Obs: {item.SpecialNote}]";
                    Console.WriteLine($"      - {item.Quantity} x {item.DishName}{note}");
                }
                Console.WriteLine(new string('-', 60));

                orderMap[index] = order;
                index++;
            }
        }

        Console.WriteLine("\nScrie NUMARUL comenzii pentru a schimba statusul sau '0' pt inapoi.");
        Console.Write("> ");
        var input = Console.ReadLine();
        if (input == "0") break;

        if (int.TryParse(input, out int sel) && orderMap.ContainsKey(sel))
        {
            var selectedOrder = orderMap[sel];
            
            Console.WriteLine($"\n--- Modificare Comanda #{sel} ---");
            Console.WriteLine("1. Primita (Created)");
            Console.WriteLine("2. In Preparare (Preparing)");
            Console.WriteLine("3. Gata de Livrare (Ready)");
            Console.WriteLine("4. Finalizata (Completed)");
            Console.WriteLine("5. ANULEAZA (Canceled)");
            Console.Write("Status nou: ");
            
            var sInput = Console.ReadLine();
            OrderStatus? newStatus = sInput switch 
            {
                "1" => OrderStatus.Created, "2" => OrderStatus.Preparing, "3" => OrderStatus.Ready,
                "4" => OrderStatus.Completed, "5" => OrderStatus.Canceled, _ => null
            };

            if (newStatus.HasValue)
            {
                if (newStatus == OrderStatus.Canceled && selectedOrder.Status == OrderStatus.Completed)
                {
                    Console.WriteLine("EROARE: Nu poti anula o comanda finalizata!");
                }
                else
                {
                    service.UpdateOrderStatus(selectedOrder.Id, newStatus.Value);
                    Console.WriteLine("Status actualizat!");
                }
                Console.ReadLine();
            }
        }
    }
}

static void HandleMenuMgmt(RestaurantService service)
{
    while (true)
    {
        Console.Clear();
        Console.WriteLine("--- GESTIONARE MENIU ---");
        var menu = service.GetMenu().OrderBy(d => d.Category).ToList();
        var menuMap = new Dictionary<int, Dish>();
        int idx = 1;

        Console.WriteLine(string.Format("{0,-3} | {1,-20} | {2,-8} | {3,-10}", "Nr", "Nume", "Pret", "Stoc"));
        Console.WriteLine(new string('-', 50));

        foreach (var dish in menu)
        {
            bool ok = service.IsDishAvailable(dish);
            Console.WriteLine(string.Format("{0,-3} | {1,-20} | {2,-8} | {3,-10}", idx, dish.Name, dish.Price, ok?"OK":"LIPSA"));
            menuMap[idx++] = dish;
        }

        Console.WriteLine("\n[A] Adauga | [S] Sterge | [0] Inapoi");
        var ch = Console.ReadLine()?.ToUpper();
        if (ch == "0") break;

        if (ch == "A")
        {
            Console.Write("Nume: "); var n = Console.ReadLine();
            Console.Write("Pret: "); decimal.TryParse(Console.ReadLine(), out decimal p);
            
            Console.WriteLine("Cat: 1.Main, 2.Appetizer, 3.Dessert, 4.Drink");
            var cInput = Console.ReadLine();
            var cat = cInput switch { "2"=>DishCategory.Appetizer, "3"=>DishCategory.Dessert, "4"=>DishCategory.Beverage, _=>DishCategory.MainCourse};

            var recipe = new List<RecipeItem>();
            var inv = service.GetInventory().ToList();
            
            if(!inv.Any()) Console.WriteLine("Nu ai ingrediente in stoc! Reteta va fi goala.");
            else 
            {
                while(true) {
                    Console.WriteLine("\n--- Ingrediente ---");
                    for(int i=0;i<inv.Count;i++) Console.WriteLine($"{i+1}. {inv[i].Name} ({inv[i].Unit})");
                    Console.Write("Alege nr (sau 0 gata): ");
                    if(int.TryParse(Console.ReadLine(), out int x) && x>0 && x<=inv.Count) {
                        Console.Write("Cantitate necesara: ");
                        if(double.TryParse(Console.ReadLine(), out double q))
                            recipe.Add(new RecipeItem{IngredientId=inv[x-1].Id, IngredientName=inv[x-1].Name, QuantityRequired=q});
                    } else if (x==0) break;
                }
            }
            service.AddDish(n, p, cat, recipe);
        }

        if (ch == "S")
        {
            Console.Write("Nr produs de sters: ");
            if (int.TryParse(Console.ReadLine(), out int s) && menuMap.ContainsKey(s))
            {
                service.RemoveDish(menuMap[s].Id);
                Console.WriteLine("Sters.");
                Thread.Sleep(500);
            }
        }
    }
}

static void HandleStockMgmt(RestaurantService service)
{
    while(true)
    {
        Console.Clear();
        Console.WriteLine("--- STOC INGREDIENTE ---");
        var st = service.GetInventory().ToList();
        foreach(var i in st) Console.WriteLine($"- {i.Name}: {i.StockQuantity} {i.Unit}");

        Console.WriteLine("\n1. Adauga Ingredient Nou | 2. Restock | 0. Inapoi");
        var k = Console.ReadLine();
        if(k=="0") break;
        
        if(k=="1") {
            Console.Write("Nume: "); var n=Console.ReadLine();
            Console.Write("Unitate: "); var u=Console.ReadLine();
            Console.Write("Stoc: "); double.TryParse(Console.ReadLine(), out double q);
            service.AddIngredientToStock(n,u,q);
        }
        if(k=="2") {
            Console.Write("Nume ingredient: "); var n=Console.ReadLine();
            var item = st.FirstOrDefault(x=>x.Name.Equals(n, StringComparison.OrdinalIgnoreCase));
            if(item!=null) {
                Console.Write("Stoc NOU total: ");
                if(double.TryParse(Console.ReadLine(), out double q)) service.UpdateStock(item.Id, q);
            } else Console.WriteLine("Nu exista.");
        }
    }
}

static void HandleDeliveryConfig(RestaurantService service)
{
    while(true)
    {
        Console.Clear();
        var s = service.GetSettings();
        Console.WriteLine("--- CONFIGURARE LIVRARE ---");
        Console.WriteLine($"1. Taxa Livrare: {s.DeliveryCost} RON");
        Console.WriteLine($"2. Timp Estimat: {s.DeliveryTimeMinutes} min");
        Console.WriteLine($"3. Comanda Minima: {s.MinimumOrderAmount} RON");
        Console.WriteLine("0. Inapoi");
        
        Console.Write("Modifica (1-3): ");
        var ch = Console.ReadLine();
        if(ch=="0") break;
        
        decimal c=s.DeliveryCost; int t=s.DeliveryTimeMinutes; decimal m=s.MinimumOrderAmount;
        
        if(ch=="1") { Console.Write("Noua taxa: "); decimal.TryParse(Console.ReadLine(), out c); }
        if(ch=="2") { Console.Write("Nou timp: "); int.TryParse(Console.ReadLine(), out t); }
        if(ch=="3") { Console.Write("Nou minim: "); decimal.TryParse(Console.ReadLine(), out m); }
        
        service.UpdateSettings(c,t,m);
        Console.WriteLine("Salvat.");
        Thread.Sleep(500);
    }
}

static void SeedData(RestaurantService s, IRepository<User> u, IRepository<Ingredient> i, IRepository<RestaurantSettings> set)
{
    if (!u.GetAll().Any()) 
    {
        u.Add(new Manager("admin", "admin"));
        u.Add(new Client("client", "pass", "Strada Test 1"));
    }
    
    if (!i.GetAll().Any())
    {
        s.AddIngredientToStock("Faina", "kg", 20);
        s.AddIngredientToStock("Mozzarella", "kg", 10);
        s.AddIngredientToStock("Sos Rosii", "l", 5);
        s.AddIngredientToStock("Apa", "l", 50);
        s.AddIngredientToStock("Carne Vita", "kg", 10);
        s.AddIngredientToStock("Chifla", "buc", 20);
    }
    
    if (!s.GetMenu().Any())
    {
        var faina = i.GetAll().FirstOrDefault(x => x.Name == "Faina");
        var mozza = i.GetAll().FirstOrDefault(x => x.Name == "Mozzarella");
        var carne = i.GetAll().FirstOrDefault(x => x.Name == "Carne Vita");
        var chifla = i.GetAll().FirstOrDefault(x => x.Name == "Chifla");

        if (faina != null && mozza != null)
        {
            var retetaPizza = new List<RecipeItem>
            {
                new RecipeItem { IngredientId = faina.Id, IngredientName = "Faina", QuantityRequired = 0.3 },
                new RecipeItem { IngredientId = mozza.Id, IngredientName = "Mozzarella", QuantityRequired = 0.15 }
            };
            s.AddDish("Pizza Margherita", 35, DishCategory.MainCourse, retetaPizza);
        }

        if (carne != null && chifla != null)
        {
            var retetaBurger = new List<RecipeItem>
            {
                new RecipeItem { IngredientId = carne.Id, IngredientName = "Carne Vita", QuantityRequired = 0.2 },
                new RecipeItem { IngredientId = chifla.Id, IngredientName = "Chifla", QuantityRequired = 1 }
            };
            s.AddDish("Burger Vita", 45, DishCategory.MainCourse, retetaBurger);
        }
        
        s.AddDish("Apa Plata", 10, DishCategory.Beverage, new List<RecipeItem>());
    }
    
    s.GetSettings();
}
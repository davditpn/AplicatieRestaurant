using Microsoft.Extensions.Logging;
using AplicatieRestaurant.Domain.Entities;
using AplicatieRestaurant.Domain.Enums;
using AplicatieRestaurant.Domain.Interfaces;
using RestaurantApp.Domain.Entities;

namespace RestaurantApp.Application;

public class RestaurantService
{
    private readonly IRepository<Dish> _dishRepo;
    private readonly IRepository<Order> _orderRepo;
    private readonly IRepository<User> _userRepo;
    private readonly IRepository<Ingredient> _ingredientRepo;
    private readonly IRepository<RestaurantSettings> _settingsRepo; 
    private readonly ILogger<RestaurantService> _logger;

    public RestaurantService(
        IRepository<Dish> dishRepo, 
        IRepository<Order> orderRepo, 
        IRepository<User> userRepo,
        IRepository<Ingredient> ingredientRepo,
        IRepository<RestaurantSettings> settingsRepo,
        ILogger<RestaurantService> logger)
    {
        _dishRepo = dishRepo;
        _orderRepo = orderRepo;
        _userRepo = userRepo;
        _ingredientRepo = ingredientRepo;
        _settingsRepo = settingsRepo;
        _logger = logger;
    }
    
    public RestaurantSettings GetSettings()
    {
        var settings = _settingsRepo.GetAll().FirstOrDefault();
        if (settings == null)
        {
            settings = new RestaurantSettings();
            _settingsRepo.Add(settings);
        }
        return settings;
    }

    public void UpdateSettings(decimal cost, int time, decimal minOrder)
    {
        var s = GetSettings();
        s.DeliveryCost = cost;
        s.DeliveryTimeMinutes = time;
        s.MinimumOrderAmount = minOrder;
        _settingsRepo.Update(s);
        _logger.LogInformation("Setări livrare actualizate.");
    }
    
    public Order PlaceOrder(Guid clientId, List<(Guid DishId, int Qty, string Note)> cartItems, bool isDelivery)
    {
        var settings = GetSettings();
        
        decimal fee = isDelivery ? settings.DeliveryCost : 0;
        
        foreach (var item in cartItems)
        {
            var dish = _dishRepo.GetById(item.DishId);
            if (dish == null) continue;

            foreach (var recipeItem in dish.Recipe)
            {
                var stockItem = _ingredientRepo.GetById(recipeItem.IngredientId);
                double totalNeeded = recipeItem.QuantityRequired * item.Qty;

                if (stockItem == null || stockItem.StockQuantity < totalNeeded)
                    throw new Exception($"Stoc insuficient pentru {dish.Name}!");
            }
        }
        
        foreach (var item in cartItems)
        {
            var dish = _dishRepo.GetById(item.DishId)!;
            foreach (var recipeItem in dish.Recipe)
            {
                var stockItem = _ingredientRepo.GetById(recipeItem.IngredientId)!;
                stockItem.StockQuantity -= (recipeItem.QuantityRequired * item.Qty);
                _ingredientRepo.Update(stockItem);
            }
        }
        
        var order = new Order(clientId, isDelivery, fee);
        
        foreach (var item in cartItems)
        {
            var dish = _dishRepo.GetById(item.DishId);
            if (dish != null) 
            {
                order.AddItem(dish, item.Qty, item.Note);
            }
        }

        _orderRepo.Add(order);
        return order;
    }
    
    public IEnumerable<Order> GetClientHistory(Guid clientId)
    {
        return _orderRepo.GetAll()
            .Where(o => o.ClientId == clientId)
            .OrderByDescending(o => o.CreatedAt);
    }
    
    public IEnumerable<Dish> GetMenu() => _dishRepo.GetAll();
    public IEnumerable<Order> GetAllOrders() => _orderRepo.GetAll();
    public IEnumerable<Ingredient> GetInventory() => _ingredientRepo.GetAll();
    
    public void AddIngredientToStock(string n, string u, double q) => _ingredientRepo.Add(new Ingredient(n,u,q));
    public void UpdateStock(Guid id, double q) { var i = _ingredientRepo.GetById(id); if(i!=null){ i.StockQuantity=q; _ingredientRepo.Update(i); }}
    public bool IsDishAvailable(Dish d) { foreach(var r in d.Recipe){ var s=_ingredientRepo.GetById(r.IngredientId); if(s==null || s.StockQuantity < r.QuantityRequired) return false; } return true; }
    public void AddDish(string n, decimal p, DishCategory c, List<RecipeItem> r) => _dishRepo.Add(new Dish(n,"Desc",p,c,r));
    public void RemoveDish(Guid id) => _dishRepo.Delete(id);
    public User? Login(string u, string p) => _userRepo.GetAll().FirstOrDefault(x => x.Username==u && x.Password==p);
    public bool RegisterClient(string u, string p, string a) { if(_userRepo.GetAll().Any(x=>x.Username==u))return false; _userRepo.Add(new Client(u,p,a)); return true; }
    
    public void UpdateOrderStatus(Guid oid, OrderStatus s) {
        var o = _orderRepo.GetById(oid); if(o==null)return;
        switch(s){
            case OrderStatus.Preparing: o.MarkAsPreparing(); break;
            case OrderStatus.Ready: o.MarkAsReady(); break;
            case OrderStatus.Completed: o.CompleteOrder(); break;
            case OrderStatus.Canceled: o.CancelOrder(); break;
        }
        _orderRepo.Update(o);
    }
}
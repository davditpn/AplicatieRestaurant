using Microsoft.Extensions.Logging;
using AplicatieRestaurant.Domain.Entities;
using AplicatieRestaurant.Domain.Enums;
using AplicatieRestaurant.Domain.Interfaces;
using AplicatieRestaurant.Domain.Entities;

namespace AplicatieRestaurant.Application;

public class RestaurantService
{
    private readonly IRepository<Dish> _dishRepo;
    private readonly IRepository<Order> _orderRepo;
    private readonly IRepository<User> _userRepo;
    private readonly IRepository<Ingredient> _ingredientRepo;
    private readonly ILogger<RestaurantService> _logger;

    public RestaurantService(
        IRepository<Dish> dishRepo, 
        IRepository<Order> orderRepo, 
        IRepository<User> userRepo,
        IRepository<Ingredient> ingredientRepo,
        ILogger<RestaurantService> logger)
    {
        _dishRepo = dishRepo;
        _orderRepo = orderRepo;
        _userRepo = userRepo;
        _ingredientRepo = ingredientRepo;
        _logger = logger;
    }
    
    public IEnumerable<Dish> GetMenu() => _dishRepo.GetAll();
    public IEnumerable<Order> GetAllOrders() => _orderRepo.GetAll();
    public IEnumerable<Ingredient> GetInventory() => _ingredientRepo.GetAll();
    
    public void AddIngredientToStock(string name, string unit, double qty)
    {
        var ing = new Ingredient(name, unit, qty);
        _ingredientRepo.Add(ing);
    }

    public void UpdateStock(Guid ingredientId, double newQty)
    {
        var ing = _ingredientRepo.GetById(ingredientId);
        if (ing != null)
        {
            ing.StockQuantity = newQty;
            _ingredientRepo.Update(ing);
        }
    }
    
    public bool IsDishAvailable(Dish dish)
    {
        foreach (var item in dish.Recipe)
        {
            var stockItem = _ingredientRepo.GetById(item.IngredientId);
            if (stockItem == null || stockItem.StockQuantity < item.QuantityRequired)
            {
                return false;
            }
        }
        return true;
    }
    
    public void AddDish(string name, decimal price, DishCategory category, List<RecipeItem> recipe)
    {
        var dish = new Dish(name, "Standard", price, category, recipe);
        _dishRepo.Add(dish);
    }

    public void RemoveDish(Guid dishId) => _dishRepo.Delete(dishId);
    
    public Order PlaceOrder(Guid clientId, Dictionary<Guid, int> cartItems)
    {
        foreach (var item in cartItems)
        {
            var dish = _dishRepo.GetById(item.Key);
            if (dish == null) continue;

            int quantityOrdered = item.Value;
            
            foreach (var recipeItem in dish.Recipe)
            {
                var stockItem = _ingredientRepo.GetById(recipeItem.IngredientId);
                double totalNeeded = recipeItem.QuantityRequired * quantityOrdered;

                if (stockItem == null || stockItem.StockQuantity < totalNeeded)
                {
                    throw new Exception($"Stoc insuficient pentru {dish.Name}! Lipsește: {recipeItem.IngredientName}");
                }
            }
        }
        
        foreach (var item in cartItems)
        {
            var dish = _dishRepo.GetById(item.Key);
            int qty = item.Value;

            foreach (var recipeItem in dish.Recipe)
            {
                var stockItem = _ingredientRepo.GetById(recipeItem.IngredientId)!;
                stockItem.StockQuantity -= (recipeItem.QuantityRequired * qty);
                _ingredientRepo.Update(stockItem);
            }
        }
        
        var order = new Order(clientId);
        foreach (var item in cartItems)
        {
            var dish = _dishRepo.GetById(item.Key);
            
            if (dish != null) 
            {
                order.AddItem(dish!, item.Value); 
            }
        }

        _orderRepo.Add(order);
        _logger.LogInformation("Comandă plasată...");
        return order;
    }
    
    public User? Login(string username, string password)
    {
        var user = _userRepo.GetAll().FirstOrDefault(u => u.Username == username);
        return (user != null && user.Password == password) ? user : null;
    }

    public bool RegisterClient(string username, string password, string address)
    {
        if (_userRepo.GetAll().Any(u => u.Username == username)) return false;
        _userRepo.Add(new Client(username, password, address));
        return true;
    }

    public void UpdateOrderStatus(Guid id, OrderStatus status) 
    {
        var o = _orderRepo.GetById(id);
        if (o == null) return;
        if (status == OrderStatus.Preparing) o.MarkAsPreparing();
        if (status == OrderStatus.Ready) o.MarkAsReady();
        if (status == OrderStatus.Completed) o.CompleteOrder();
        _orderRepo.Update(o);
    }
    
    public void DeleteOrder(Guid id) => _orderRepo.Delete(id);
}
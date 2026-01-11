using Microsoft.Extensions.Logging;
using AplicatieRestaurant.Domain.Entities;
using AplicatieRestaurant.Domain.Enums;
using AplicatieRestaurant.Domain.Interfaces;

namespace AplicatieRestaurant.Application;

public class RestaurantService
{
    private readonly IRepository<Dish> _dishRepo;
    private readonly IRepository<Order> _orderRepo;
    private readonly ILogger<RestaurantService> _logger;

    public RestaurantService(IRepository<Dish> dishRepo, IRepository<Order> orderRepo,
        ILogger<RestaurantService> logger)
    {
        _dishRepo = dishRepo;
        _orderRepo = orderRepo;
        _logger = logger;
    }

    public IEnumerable<Dish> GetMenu() => _dishRepo.GetAll();
    public IEnumerable<Order> GetAllOrders() => _orderRepo.GetAll();

    public void AddDish(string name, decimal price, DishCategory category)
    {
        var dish = new Dish(name, "Descriere standard", price, category, new List<Ingredient>());
        _dishRepo.Add(dish);
        _logger.LogInformation("Manager: Produs adăugat {Name}", name);
    }

    public Order PlaceOrder(Guid clientId, Dictionary<Guid, int> cartItems)
    {
        var order = new Order(clientId);
        foreach (var item in cartItems)
        {
            var dish = _dishRepo.GetById(item.Key);
            if (dish != null) order.AddItem(dish, item.Value);
        }

        _orderRepo.Add(order);
        _logger.LogInformation("Client: Comanda {Id} plasată. Total: {Total}", order.Id, order.TotalPrice);
        return order;
    }

    public void UpdateOrderStatus(Guid orderId, OrderStatus status)
    {
        var order = _orderRepo.GetById(orderId);
        if (order == null) return;

        if (status == OrderStatus.Preparing) order.MarkAsPreparing();
        if (status == OrderStatus.Ready) order.MarkAsReady();
        if (status == OrderStatus.Completed) order.CompleteOrder();

        _orderRepo.Update(order);
        _logger.LogInformation("Manager: Comanda {Id} -> {Status}", orderId, status);
    }

    public void DeleteOrder(Guid orderId)
    {
        var order = _orderRepo.GetById(orderId);
        if (order != null)
        {
            _orderRepo.Delete(orderId);
            _logger.LogInformation("Manager: Comanda {Id} a fost ștearsă definitiv", orderId);
        }
    }
}
using AplicatieRestaurant.Domain.Interfaces;

namespace AplicatieRestaurant.Domain.Entities;
using System.Text.Json.Serialization;
using AplicatieRestaurant.Domain.Enums;

public class Order : IEntity
{
    [JsonInclude]
    public Guid Id { get; private set; }

    [JsonInclude] 
    public Guid ClientId { get; private set; }
    
    [JsonInclude]
    public DateTime CreatedAt { get; private set; }
    
    [JsonInclude]
    public OrderStatus Status { get; private set; }

    [JsonInclude]
    public decimal TotalPrice { get; private set; }

    [JsonInclude] public List<OrderItem> Items { get; private set; } = new();


    public Order(Guid clientId)
    {
        Id = Guid.NewGuid();
        ClientId = clientId;
        CreatedAt = DateTime.UtcNow;
        Status = OrderStatus.Created;
        TotalPrice = 0;
        Items = new List<OrderItem>();
    }
    
    [JsonConstructor]
    public Order()
    {}


    public void AddItem(Dish dish, int quantity)
    {
        if (Status != OrderStatus.Created)
        {
            throw new InvalidOperationException("Eroare la status-ul comenzii!");
        }
        Items.Add(new OrderItem(dish.Id, dish.Name, dish.Price, quantity));
        RecalculateTotal();
    }

    public void MarkAsPreparing()
    {
        Status = OrderStatus.Preparing;
    }

    public void MarkAsReady()
    {
        Status = OrderStatus.Ready;
    }

    public void CompleteOrder()
    {
        Status = OrderStatus.Completed;
    }

    private void RecalculateTotal()
    {
        TotalPrice = Items.Sum(i => i.Price * i.Quantity);
    }
}
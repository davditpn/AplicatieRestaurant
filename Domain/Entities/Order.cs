using System.Text.Json.Serialization;
using AplicatieRestaurant.Domain.Entities;
using AplicatieRestaurant.Domain.Enums;
using AplicatieRestaurant.Domain.Interfaces;

namespace AplicatieRestaurant.Domain.Entities;

public class Order : IEntity
{
    [JsonInclude] public Guid Id { get; private set; }
    [JsonInclude] public Guid ClientId { get; private set; }
    [JsonInclude] public DateTime CreatedAt { get; private set; }
    [JsonInclude] public OrderStatus Status { get; private set; }
    [JsonInclude] public decimal TotalPrice { get; private set; }
    [JsonInclude] public List<OrderItem> Items { get; private set; } = new();
    
    [JsonInclude] public bool IsDelivery { get; private set; }
    [JsonInclude] public decimal DeliveryFee { get; private set; }

    public Order(Guid clientId, bool isDelivery, decimal deliveryFee)
    {
        Id = Guid.NewGuid();
        ClientId = clientId;
        CreatedAt = DateTime.UtcNow;
        Status = OrderStatus.Created;
        Items = new List<OrderItem>();
        
        IsDelivery = isDelivery;
        DeliveryFee = deliveryFee;
        TotalPrice = 0; 
    }

    [JsonConstructor]
    public Order() { }

    public void AddItem(Dish dish, int quantity, string note)
    {
        if (Status != OrderStatus.Created) throw new InvalidOperationException("Comanda blocata.");
        
        Items.Add(new OrderItem(dish.Id, dish.Name, dish.Price, quantity, note));
        RecalculateTotal();
    }
    
    public void MarkAsPreparing() => Status = OrderStatus.Preparing;
    public void MarkAsReady() => Status = OrderStatus.Ready;
    public void CompleteOrder() => Status = OrderStatus.Completed;
    public void CancelOrder() { if (Status != OrderStatus.Completed) Status = OrderStatus.Canceled; }

    private void RecalculateTotal()
    {
        decimal itemsTotal = Items.Sum(i => i.Price * i.Quantity);
        TotalPrice = itemsTotal + DeliveryFee;
    }
}
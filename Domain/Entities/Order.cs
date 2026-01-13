using System.Text.Json.Serialization;
using AplicatieRestaurant.Domain.Entities;
using AplicatieRestaurant.Domain.Enums;
using AplicatieRestaurant.Domain.Interfaces;

namespace RestaurantApp.Domain.Entities;

public class Order : IEntity
{
    [JsonInclude] public Guid Id { get; private set; }
    [JsonInclude] public Guid ClientId { get; private set; }
    [JsonInclude] public DateTime CreatedAt { get; private set; }
    [JsonInclude] public OrderStatus Status { get; private set; }
    [JsonInclude] public decimal TotalPrice { get; private set; }
    [JsonInclude] public List<OrderItem> Items { get; private set; } = new();

    // --- CÂMPURI NOI PENTRU LIVRARE ---
    [JsonInclude] public bool IsDelivery { get; private set; } // True=Livrare, False=Ridicare
    [JsonInclude] public decimal DeliveryFee { get; private set; } // Taxa aplicată

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
        if (Status != OrderStatus.Created) throw new InvalidOperationException("Comanda blocată.");
        
        Items.Add(new OrderItem(dish.Id, dish.Name, dish.Price, quantity, note));
        RecalculateTotal();
    }

    // Metodele de status
    public void MarkAsPreparing() => Status = OrderStatus.Preparing;
    public void MarkAsReady() => Status = OrderStatus.Ready;
    public void CompleteOrder() => Status = OrderStatus.Completed;
    public void CancelOrder() { if (Status != OrderStatus.Completed) Status = OrderStatus.Canceled; }

    private void RecalculateTotal()
    {
        // Total = Produse + Taxa Livrare (daca e cazul)
        decimal itemsTotal = Items.Sum(i => i.Price * i.Quantity);
        TotalPrice = itemsTotal + DeliveryFee;
    }
}
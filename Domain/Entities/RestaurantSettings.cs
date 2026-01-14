using AplicatieRestaurant.Domain.Interfaces;

namespace AplicatieRestaurant.Domain.Entities;

public class RestaurantSettings : IEntity
{
    public Guid Id { get; init; } = Guid.NewGuid();
    
    public decimal DeliveryCost { get; set; } = 15.0m;
    public int DeliveryTimeMinutes { get; set; } = 45;
    public decimal MinimumOrderAmount { get; set; } = 30.0m;
}
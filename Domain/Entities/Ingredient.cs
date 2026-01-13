using AplicatieRestaurant.Domain.Interfaces;
using AplicatieRestaurant.Domain.Interfaces;

namespace RestaurantApp.Domain.Entities;

public class Ingredient : IEntity
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; set; }
    public string Unit { get; set; }
    public double StockQuantity { get; set; }

    public Ingredient() { }

    public Ingredient(string name, string unit, double initialStock)
    {
        Name = name;
        Unit = unit;
        StockQuantity = initialStock;
    }
}
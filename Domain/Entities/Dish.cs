using AplicatieRestaurant.Domain.Enums;
using AplicatieRestaurant.Domain.Interfaces;

namespace AplicatieRestaurant.Domain.Entities;

public class RecipeItem
{
    public Guid IngredientId { get; set; }
    public string IngredientName { get; set; }
    public double QuantityRequired { get; set; }
}

public record Dish : IEntity
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; init; }
    public string Description { get; init; }
    public decimal Price { get; init; }
    public DishCategory Category { get; init; }
    
    public List<RecipeItem> Recipe { get; init; } = new();

    public Dish() { }

    public Dish(string name, string description, decimal price, DishCategory category, List<RecipeItem> recipe)
    {
        Name = name;
        Description = description;
        Price = price;
        Category = category;
        Recipe = recipe;
    }
}
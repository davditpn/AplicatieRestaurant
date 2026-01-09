namespace AplicatieRestaurant.Domain.Entities;
using System.Text.Json.Serialization;
using AplicatieRestaurant.Domain.Enums;

public record Ingredient(string Name, string Unit);
public record Dish
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; init; }
    public string Description { get; init; }
    public decimal Price { get; init; }
    public DishCategory Category { get; init; }
    public IReadOnlyList<Ingredient> Ingredients { get; init; }

    public Dish(string name, string description, decimal price, DishCategory category, List<Ingredient> ingredients)
    {
        Name = name;
        Description = description;
        Price = price;
        Category = category;
        Ingredients = ingredients.AsReadOnly();
    }

    [JsonConstructor]
    public Dish(Guid id, string name, string description, decimal price, DishCategory category,
        List<Ingredient> ingredients)
    {
        Id = id;
        Name = name;
        Description = description;
        Price = price;
        Category = category;
        Ingredients = ingredients ?? new List<Ingredient>();
    }
}
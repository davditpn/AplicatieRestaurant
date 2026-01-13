namespace AplicatieRestaurant.Domain.Entities;

public class OrderItem
{
    public Guid DishId { get; set; }
    public string DishName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }

    public OrderItem() { }

    public OrderItem(Guid dishId, string dishName, decimal price, int quantity)
    {
        DishId = dishId;
        DishName = dishName;
        Price = price;
        Quantity = quantity;
    }
}
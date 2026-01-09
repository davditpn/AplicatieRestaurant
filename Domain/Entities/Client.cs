namespace AplicatieRestaurant.Domain.Entities;
using System.Text.Json.Serialization;
using AplicatieRestaurant.Domain.Enums;

public class Client : User
{
    public string DeliveryAddress { get; private set; }
    public Client(string username, string password, string address) : base(username, password, UserRole.Client)
    {
        DeliveryAddress = address;
    }

    [JsonConstructor]
    public Client(Guid id, string username, string password, UserRole role, string deliveryAddress) : base(id, username,
        password, role)
    {
        DeliveryAddress = deliveryAddress;
    }
}
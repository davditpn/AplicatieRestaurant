namespace AplicatieRestaurant.Domain.Entities;
using System.Text.Json.Serialization;
using AplicatieRestaurant.Domain.Enums;

public class Manager : User
{
    public Manager(string username, string password) : base(username, password, UserRole.Manager)
    {
        
    }

    [JsonConstructor]
    public Manager(Guid id, string username, string password, UserRole role) : base(id, username, password, role)
    {
        
    }
}
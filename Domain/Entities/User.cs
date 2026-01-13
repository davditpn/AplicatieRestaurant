using AplicatieRestaurant.Domain.Interfaces;

namespace AplicatieRestaurant.Domain.Entities;
using System.Text.Json.Serialization;
using AplicatieRestaurant.Domain.Enums;

[JsonDerivedType(typeof(Client), typeDiscriminator: "client")]
[JsonDerivedType(typeof(Manager), typeDiscriminator: "manager")]
public abstract class User : IEntity
{
    public Guid Id { get; protected set; }
    public string Username { get; protected set; }
    public string Password { get; protected set; }
    public UserRole Role { get; protected set; }
    
    protected User(string username, string password, UserRole role)
    {
        Id = Guid.NewGuid();
        Username = username;
        Password = password;
        Role = role;
    }

    protected User(Guid id, string username, string password, UserRole role)
    {
        Id = id;
        Username = username;
        Password = password;
        Role = role;
    }
}
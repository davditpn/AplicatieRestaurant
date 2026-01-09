namespace AplicatieRestaurant.Domain.Entities;
using System.Text.Json.Serialization;
using AplicatieRestaurant.Domain.Enums;

[JsonDerivedType(typeof(Client), typeDiscriminator: "client")]
[JsonDerivedType(typeof(Manager), typeDiscriminator: "manager")]
public abstract class User
{
    public Guid Id { get; protected set; }
    public string Username { get; protected set; }
    public string Password { get; protected set; }
    public UserRole Role { get; protected set; }
    
    protected User(string username, string password, UserRole role) //constructor pt creare unde id-ul se da automat
    {
        Id = Guid.NewGuid();
        Username = username;
        Password = password;
        Role = role;
    }

    protected User(Guid id, string username, string password, UserRole role) //constructor pt json cand exista deja un id
    {
        Id = id;
        Username = username;
        Password = password;
        Role = role;
    }
}
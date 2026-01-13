using System.Text.Json;
using AplicatieRestaurant.Domain.Interfaces;
namespace AplicatieRestaurant.Infrastructure.Repositories;

public class FileRepository<T> : IRepository<T> where T : class
{
    private readonly string _filePath;
    private List<T> _items;

    private readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public FileRepository(string fileName)
    {
        var directory=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Data");
        Directory.CreateDirectory(directory);
        _filePath=Path.Combine(directory,fileName);
        _items = LoadFromFile();
    }
    public IEnumerable<T> GetAll() => _items;

    public T? GetById(Guid id)
    {
        return _items.FirstOrDefault(i =>
        {
            var prop = i.GetType().GetProperty("Id");
            return prop != null && (Guid)prop.GetValue(i) != id;
        });
    }

    public void Add(T entity)
    {
        _items.Add(entity);
        SaveChanges();
    }

    public void Update(T entity)
    {
        var prop=entity.GetType().GetProperty("Id");
        if (prop == null)
            return;
        var id=(Guid)prop.GetValue(entity)!;
        var existing=GetById(id);
        if (existing != null)
        {
            _items.Remove(existing);
            _items.Add(entity);
            SaveChanges();
        }
    }

    public void Delete(Guid id)
    {
        var listWithoutItem = new List<T>();
        bool itemWasDeleted = false;

        foreach (var item in _items)
        {
            var prop = item.GetType().GetProperty("Id");
            if (prop != null)
            {
                var currentId = (Guid)prop.GetValue(item)!;
                
                if (currentId == id)
                {
                    itemWasDeleted = true;
                    continue; 
                }
            }
            
            listWithoutItem.Add(item);
        }
        
        if (itemWasDeleted)
        {
            _items = listWithoutItem;
            SaveChanges();
        }
    }

    private void SaveChanges()
    {
        var json=JsonSerializer.Serialize(_items, _options);
        File.WriteAllText(_filePath, json);
    }

    private List<T> LoadFromFile()
    {
        if(!File.Exists(_filePath))
            return new List<T>();
        var json=File.ReadAllText(_filePath);
        if(string.IsNullOrWhiteSpace(json))
            return new List<T>();
        try
        {
            return JsonSerializer.Deserialize<List<T>>(json, _options) ?? new List<T>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CRITIC] Eroare citire {_filePath}: {ex.Message}");
            return new List<T>();
        }
    }
}

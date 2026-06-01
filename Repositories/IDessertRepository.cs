using AspireApp1.Server.Models;

namespace AspireApp1.Server.Repositories;

public interface IDessertRepository
{
    List<Dessert> GetAll(string? search = null);
    Dessert? GetById(int id);
    Dessert Add(Dessert dessert);
    Dessert? Update(int id, Dessert updated);
    bool Delete(int id);
}
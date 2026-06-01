using AspireApp1.Server.Models;

namespace AspireApp1.Server.Repositories;

public interface IOrderRepository
{
    List<Order> GetAll();
    List<Order> GetByUserId(int userId);
    Order? GetById(int id);
    List<Order> GetAllIncludingArchived();
    List<Order> GetSince(DateTime sinceDate);
    Order Add(Order order);
    bool Delete(int id);
    bool Archive(int id);
}

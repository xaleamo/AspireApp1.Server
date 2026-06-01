using AspireApp1.Server.Models;

namespace AspireApp1.Server.Repositories;

public interface IUserRepository
{
    User? GetByEmail(string email);
    User? GetById(int id);
    List<User> GetAll();
    bool EmailExists(string email);
    User Add(User user);
}

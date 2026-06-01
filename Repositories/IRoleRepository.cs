using AspireApp1.Server.Models;

namespace AspireApp1.Server.Repositories;

public interface IRoleRepository
{
    Role? GetByName(string name);
}

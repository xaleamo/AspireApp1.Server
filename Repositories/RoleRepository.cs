using AspireApp1.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace AspireApp1.Server.Repositories
{
    public class RoleRepository : IRoleRepository
    {
        private readonly AppDbContext _context;

        public RoleRepository(AppDbContext context)
        {
            _context = context;
        }

        public Role? GetByName(string name)
        {
            return _context.Roles
                .Include(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                .FirstOrDefault(r => r.Name == name);
        }
    }
}

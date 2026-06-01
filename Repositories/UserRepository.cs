using AspireApp1.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace AspireApp1.Server.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        public User? GetByEmail(string email)
        {
            return _context.Users
                .Include(u => u.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
                .FirstOrDefault(u => u.Email == email);
        }

        public User? GetById(int id)
        {
            return _context.Users
                .Include(u => u.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
                .FirstOrDefault(u => u.Id == id);
        }

        public List<User> GetAll()
        {
            return _context.Users
                .Include(u => u.Role)
                .ToList();
        }

        public bool EmailExists(string email)
        {
            return _context.Users.Any(u => u.Email == email);
        }

        public User Add(User user)
        {
            _context.Users.Add(user);
            _context.SaveChanges();
            return user;
        }
    }
}

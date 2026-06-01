using AspireApp1.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace AspireApp1.Server.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly AppDbContext _context;

        public OrderRepository(AppDbContext context)
        {
            _context = context;
        }

        public List<Order> GetAll()
        {
            return _context.Orders
                .Include(o => o.Dessert)
                .Include(o => o.User)
                .Where(o => !o.Archived)
                .ToList();
        }

        public List<Order> GetByUserId(int userId)
        {
            return _context.Orders
                .Include(o => o.Dessert)
                .Include(o => o.User)
                .Where(o => o.UserId == userId && !o.Archived)
                .ToList();
        }

        public Order? GetById(int id)
        {
            return _context.Orders
                .Include(o => o.Dessert)
                .Include(o => o.User)
                .FirstOrDefault(o => o.Id == id);
        }

        public List<Order> GetAllIncludingArchived()
        {
            return _context.Orders
                .Include(o => o.Dessert)
                .Include(o => o.User)
                .ToList();
        }

        public List<Order> GetSince(DateTime sinceDate)
        {
            return _context.Orders
                .Include(o => o.Dessert)
                .Include(o => o.User)
                .Where(o => o.OrderedAt >= sinceDate && !o.Archived)
                .ToList();
        }

        public Order Add(Order order)
        {
            if (order.Id < 0)
            {
                order.Id = 0;
            }
            _context.Orders.Add(order);
            _context.SaveChanges();

            return _context.Orders
                .Include(o => o.Dessert)
                .Include(o => o.User)
                .First(o => o.Id == order.Id);
        }

        public bool Delete(int id)
        {
            Order? existing = _context.Orders.Find(id);
            if (existing == null) return false;
            _context.Orders.Remove(existing);
            _context.SaveChanges();
            return true;
        }

        public bool Archive(int id)
        {
            Order? existing = _context.Orders.Find(id);
            if (existing == null) return false;
            existing.Archived = true;
            _context.SaveChanges();
            return true;
        }
    }
}

using AspireApp1.Server.DTO;
using Microsoft.EntityFrameworkCore;

namespace AspireApp1.Server.Repositories
{
    public class StatisticsRepository : IStatisticsRepository
    {
        private readonly AppDbContext _context;

        public StatisticsRepository(AppDbContext context)
        {
            _context = context;
        }

        public List<TopDessertDto> GetTopDesserts(DateTime since, int count)
        {
            return _context.Orders
                .AsNoTracking()
                .Where(o => o.OrderedAt >= since)
                .GroupBy(o => o.Dessert.Name)
                .Select(g => new TopDessertDto { Name = g.Key, Orders = g.Count() })
                .OrderByDescending(d => d.Orders)
                .Take(count)
                .ToList(); // Synchronous
        }

        public List<CustomerOrdersDto> GetOrdersPerCustomer()
        {
            return _context.Orders
                .AsNoTracking()
                .GroupBy(order => new { order.User.Email, order.User.FirstName,order.User.Surname })
                .Select(grouping => new CustomerOrdersDto
                {
                    CustomerEmail = grouping.Key.Email,
                    CustomerName = grouping.Key.Surname+" "+grouping.Key.FirstName,
                    Orders = grouping.Count()
                })
                .OrderByDescending(c => c.Orders)
                .ToList();
        }

        public List<DessertOrdersDto> GetOrdersPerDessert()
        {
            return _context.Orders
                .AsNoTracking()
                .GroupBy(o => o.Dessert.Name)
                .Select(g => new DessertOrdersDto
                {
                    DessertName = g.Key,
                    Orders = g.Count()
                })
                .OrderByDescending(d => d.Orders)
                .ToList();
        }

        public int GetOrderCount(DateTime since)
        {
            return _context.Orders
                .AsNoTracking()
                .Count(o => o.OrderedAt >= since);
        }
    }
}
using AspireApp1.Server.Models;

namespace AspireApp1.Server.Repositories
{
    public class OrderInMemoryRepository : IOrderRepository
    {
        private readonly List<Order> _orders = new();
        private int _nextId = 1;

        public List<Order> GetAll() => _orders.Where(o => !o.Archived).ToList();

        public List<Order> GetByUserId(int userId) =>
            _orders.Where(o => o.UserId == userId && !o.Archived).ToList();

        public Order? GetById(int id) => _orders.FirstOrDefault(o => o.Id == id);

        public List<Order> GetAllIncludingArchived() => _orders.ToList();

        public List<Order> GetSince(DateTime sinceDate) =>
            _orders.Where(o => o.OrderedAt >= sinceDate).ToList();

        public Order Add(Order order)
        {
            order.Id = _nextId++;
            _orders.Add(order);
            return order;
        }

        public bool Delete(int id)
        {
            var order = GetById(id);
            if (order == null) return false;
            _orders.Remove(order);
            return true;
        }

        public bool Archive(int id)
        {
            var order = GetById(id);
            if (order == null) return false;
            order.Archived = true;
            return true;
        }
    }
}

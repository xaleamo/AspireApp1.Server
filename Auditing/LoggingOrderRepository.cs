using AspireApp1.Server.Models;
using AspireApp1.Server.Repositories;

namespace AspireApp1.Server.Auditing
{
    public class LoggingOrderRepository : IOrderRepository
    {
        private const string Repo = nameof(OrderRepository);
        private const string Entity = nameof(Order);

        private readonly IOrderRepository _inner;
        private readonly IActionLogger _logger;

        public LoggingOrderRepository(IOrderRepository inner, IActionLogger logger)
        {
            _inner = inner;
            _logger = logger;
        }

        public List<Order> GetAll() =>
            Audit($"{Repo}.{nameof(GetAll)}", null, null, () => _inner.GetAll());

        public List<Order> GetByUserId(int userId) =>
            Audit($"{Repo}.{nameof(GetByUserId)}",
                details: $"userId={userId}", entityId: null,
                () => _inner.GetByUserId(userId));

        public Order? GetById(int id) =>
            Audit($"{Repo}.{nameof(GetById)}", null, id.ToString(),
                () => _inner.GetById(id));

        public List<Order> GetAllIncludingArchived() =>
            Audit($"{Repo}.{nameof(GetAllIncludingArchived)}", null, null,
                () => _inner.GetAllIncludingArchived());

        public List<Order> GetSince(DateTime sinceDate) =>
            Audit($"{Repo}.{nameof(GetSince)}",
                details: $"since={sinceDate:O}", entityId: null,
                () => _inner.GetSince(sinceDate));

        public Order Add(Order order)
        {
            string action = $"{Repo}.{nameof(Add)}";
            string details = $"dessertId={order.DessertId};userId={order.UserId}";
            try
            {
                Order created = _inner.Add(order);
                _ = _logger.LogAsync(action, Entity, created.Id.ToString(), details,
                    success: true,
                    overrideUserId: created.UserId);
                return created;
            }
            catch (Exception ex)
            {
                _ = _logger.LogAsync(action, Entity, null,
                    $"{details}; error={ex.GetType().Name}", success: false);
                throw;
            }
        }

        public bool Delete(int id) =>
            Audit($"{Repo}.{nameof(Delete)}", null, id.ToString(),
                () => _inner.Delete(id));

        public bool Archive(int id) =>
            Audit($"{Repo}.{nameof(Archive)}", null, id.ToString(),
                () => _inner.Archive(id));

        private T Audit<T>(string action, string? details, string? entityId, Func<T> op)
        {
            try
            {
                T result = op();
                _ = _logger.LogAsync(action, Entity, entityId, details, success: true);
                return result;
            }
            catch (Exception ex)
            {
                _ = _logger.LogAsync(action, Entity, entityId,
                    details: $"{details}; error={ex.GetType().Name}",
                    success: false);
                throw;
            }
        }
    }
}

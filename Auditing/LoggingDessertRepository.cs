using AspireApp1.Server.Models;
using AspireApp1.Server.Repositories;

namespace AspireApp1.Server.Auditing
{
    public class LoggingDessertRepository : IDessertRepository
    {
        private const string Repo = nameof(DessertRepository);
        private const string Entity = nameof(Dessert);

        private readonly IDessertRepository _inner;
        private readonly IActionLogger _logger;

        public LoggingDessertRepository(IDessertRepository inner, IActionLogger logger)
        {
            _inner = inner;
            _logger = logger;
        }

        public List<Dessert> GetAll(string? search = null)
        {
            return Audit(
                $"{Repo}.{nameof(GetAll)}",
                details: search != null ? $"search={search}" : null,
                entityId: null,
                () => _inner.GetAll(search));
        }

        public Dessert? GetById(int id)
        {
            return Audit(
                $"{Repo}.{nameof(GetById)}",
                details: null,
                entityId: id.ToString(),
                () => _inner.GetById(id));
        }

        public Dessert Add(Dessert dessert)
        {
            return Audit(
                $"{Repo}.{nameof(Add)}",
                details: $"name={dessert.Name}",
                entityId: null,
                () =>
                {
                    Dessert created = _inner.Add(dessert);
                    return (created, created.Id.ToString());
                });
        }

        public Dessert? Update(int id, Dessert updated)
        {
            return Audit(
                $"{Repo}.{nameof(Update)}",
                details: $"name={updated.Name}",
                entityId: id.ToString(),
                () => _inner.Update(id, updated));
        }

        public bool Delete(int id)
        {
            return Audit(
                $"{Repo}.{nameof(Delete)}",
                details: null,
                entityId: id.ToString(),
                () => _inner.Delete(id));
        }

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

        private T Audit<T>(string action, string? details, string? entityId,
            Func<(T Result, string EntityId)> op)
        {
            try
            {
                (T result, string id) = op();
                _ = _logger.LogAsync(action, Entity, id, details, success: true);
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

using AspireApp1.Server.DTO;
using AspireApp1.Server.Repositories;

namespace AspireApp1.Server.Auditing
{
    public class LoggingStatisticsRepository : IStatisticsRepository
    {
        private const string Repo = nameof(StatisticsRepository);
        private const string Entity = "Statistics";

        private readonly IStatisticsRepository _inner;
        private readonly IActionLogger _logger;

        public LoggingStatisticsRepository(IStatisticsRepository inner, IActionLogger logger)
        {
            _inner = inner;
            _logger = logger;
        }

        public List<TopDessertDto> GetTopDesserts(DateTime since, int count) =>
            Audit($"{Repo}.{nameof(GetTopDesserts)}",
                details: $"since={since:O};count={count}",
                () => _inner.GetTopDesserts(since, count));

        public List<CustomerOrdersDto> GetOrdersPerCustomer() =>
            Audit($"{Repo}.{nameof(GetOrdersPerCustomer)}", null,
                () => _inner.GetOrdersPerCustomer());

        public List<DessertOrdersDto> GetOrdersPerDessert() =>
            Audit($"{Repo}.{nameof(GetOrdersPerDessert)}", null,
                () => _inner.GetOrdersPerDessert());

        public int GetOrderCount(DateTime since) =>
            Audit($"{Repo}.{nameof(GetOrderCount)}",
                details: $"since={since:O}",
                () => _inner.GetOrderCount(since));

        private T Audit<T>(string action, string? details, Func<T> op)
        {
            try
            {
                T result = op();
                _ = _logger.LogAsync(action, Entity, null, details, success: true);
                return result;
            }
            catch (Exception ex)
            {
                _ = _logger.LogAsync(action, Entity, null,
                    details: $"{details}; error={ex.GetType().Name}",
                    success: false);
                throw;
            }
        }
    }
}

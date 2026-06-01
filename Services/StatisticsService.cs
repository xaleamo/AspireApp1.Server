using AspireApp1.Server.DTO;
using AspireApp1.Server.Repositories;

namespace AspireApp1.Server.Services
{
    public class StatisticsService
    {
        private readonly IStatisticsRepository _statsRepo;

        public StatisticsService(IStatisticsRepository statsRepo)
        {
            _statsRepo = statsRepo;
        }

        public StatisticsDto GetStatistics()
        {
            var now = DateTime.UtcNow;
            var threeMonthsAgo = now.AddMonths(-3);
            var yearAgo = now.AddMonths(-12);

            return new StatisticsDto
            {
                TopDesserts3Months = _statsRepo.GetTopDesserts(threeMonthsAgo, 4),
                TopDessertsYear = _statsRepo.GetTopDesserts(yearAgo, 4),
                OrdersPerCustomer = _statsRepo.GetOrdersPerCustomer(),
                OrdersPerDessert = _statsRepo.GetOrdersPerDessert(),
                TotalOrders3Months = _statsRepo.GetOrderCount(threeMonthsAgo),
                TotalOrdersYear = _statsRepo.GetOrderCount(yearAgo)
            };
        }
    }
}

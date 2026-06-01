using AspireApp1.Server.DTO;

namespace AspireApp1.Server.Repositories;

public interface IStatisticsRepository
{
    List<TopDessertDto> GetTopDesserts(DateTime since, int count);
    List<CustomerOrdersDto> GetOrdersPerCustomer();
    List<DessertOrdersDto> GetOrdersPerDessert();
    int GetOrderCount(DateTime since);
}
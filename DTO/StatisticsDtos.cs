namespace AspireApp1.Server.DTO
{
    public class TopDessertDto
    {
        public string Name { get; set; } = "";
        public int Orders { get; set; }
    }

    public class CustomerOrdersDto
    {
        public string CustomerName { get; set; } = "";
        public string CustomerEmail { get; set; } = "";
        public int Orders { get; set; }
    }

    public class DessertOrdersDto
    {
        public string DessertName { get; set; } = "";
        public int Orders { get; set; }
    }

    public class StatisticsDto
    {
        public List<TopDessertDto> TopDesserts3Months { get; set; } = new();
        public List<TopDessertDto> TopDessertsYear { get; set; } = new();
        public List<CustomerOrdersDto> OrdersPerCustomer { get; set; } = new();
        public List<DessertOrdersDto> OrdersPerDessert { get; set; } = new();
        public int TotalOrders3Months { get; set; }
        public int TotalOrdersYear { get; set; }
    }
}

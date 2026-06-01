using AspireApp1.Server.DTO;
using AspireApp1.Server.Models;
using AspireApp1.Server.Repositories;

namespace AspireApp1.Server.Services
{
    public class OrderService
    {
        private readonly IOrderRepository _repo;

        public OrderService(IOrderRepository repo)
        {
            _repo = repo;
        }

        public List<OrderDto> GetAll() =>
            _repo.GetAll().Select(OrderMapper.ToDto).ToList();

        public List<OrderDto> GetByUserId(int userId) =>
            _repo.GetByUserId(userId).Select(OrderMapper.ToDto).ToList();

        public OrderDto? GetById(int id)
        {
            var order = _repo.GetById(id);
            return order == null ? null : OrderMapper.ToDto(order);
        }

        public OrderDto Create(CreateOrderDtoInput input)
        {
            var order = new Order
            {
                DessertId = input.DessertId,
                UserId = input.UserId,
            };
            return OrderMapper.ToDto(_repo.Add(order));
        }

        public bool Delete(int id) => _repo.Delete(id);
        public bool Archive(int id) => _repo.Archive(id);
    }
}

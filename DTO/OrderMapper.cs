using AspireApp1.Server.DTO;
using AspireApp1.Server.Models;

public static class OrderMapper
{
    public static OrderDto ToDto(Order o) => new()
    {
        Id = o.Id,
        DessertId = o.DessertId,
        DessertName = o.Dessert?.Name ?? "",
        Quantity = o.Dessert?.Quantity ?? "",
        User = new OrderUserDto
        {
            Id = o.User?.Id ?? o.UserId,
            Email = o.User?.Email ?? "",
            FirstName = o.User?.FirstName ?? "",
            Surname = o.User?.Surname ?? "",
        },
        OrderedAt = o.OrderedAt,
        Archived = o.Archived,
    };
}

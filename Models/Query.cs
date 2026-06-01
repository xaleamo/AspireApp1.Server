using AspireApp1.Server.DTO;
using AspireApp1.Server.Services;
using HotChocolate.Authorization;

namespace AspireApp1.Server.Models;

public class Query
{
    //desserts

    [Authorize(Policy = "perm:desserts:read")]
    public PagedResultDto<DessertSummaryDto> GetDesserts(
        int page,
        int pageSize,
        string? searchName,
        [Service] DessertService service)
    {
        return service.GetPaged(page, pageSize, searchName);
    }

    [Authorize(Policy = "perm:desserts:read")]
    public DessertDetailDto? GetDessertById(
        int id,
        [Service] DessertService service)
    {
        return service.GetById(id);
    }

    //orders

    // Single field, two policies resolved at runtime. Admins (orders:read:all)
    // get the result they asked for; customers (orders:read:own) are silently
    // forced to their own user id regardless of the userId argument the client
    // sent, so a customer can't probe other users' orders by passing them.
    [Authorize]
    public List<OrderDto> GetOrders(
        int? userId,
        [Service] OrderService service,
        [Service] IHttpContextAccessor http)
    {
        GraphQLAuthContext.Caller caller = GraphQLAuthContext.From(http);

        if (caller.Has("orders:read:all"))
        {
            return userId is not null
                ? service.GetByUserId(userId.Value)
                : service.GetAll();
        }

        if (caller.Has("orders:read:own") && caller.UserId is int callerId)
        {
            return service.GetByUserId(callerId);
        }

        throw new GraphQLException("Not authorized.");
    }

    [Authorize]
    public OrderDto? GetOrderById(
        int id,
        [Service] OrderService service,
        [Service] IHttpContextAccessor http)
    {
        GraphQLAuthContext.Caller caller = GraphQLAuthContext.From(http);
        OrderDto? order = service.GetById(id);
        if (order == null) return null;

        // Admin (orders:read:all) sees any order. Customer only sees their own
        // — for any other order we return null (don't leak existence).
        if (caller.Has("orders:read:all")) return order;
        if (caller.UserId is int callerId && order.User.Id == callerId) return order;
        return null;
    }

    //statistics
    [Authorize(Policy = "perm:statistics:view")]
    public StatisticsDto GetStatistics([Service] StatisticsService service)
    {
        return service.GetStatistics();
    }
}

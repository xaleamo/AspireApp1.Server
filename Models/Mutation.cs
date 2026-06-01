using AspireApp1.Server.DTO;
using AspireApp1.Server.Security;
using AspireApp1.Server.Services;
using HotChocolate.Authorization;

namespace AspireApp1.Server.Models;

public class Mutation
{
    [Authorize(Policy = "perm:desserts:create")]
    public DessertDetailDto CreateDessert(
        DessertDtoInput dto,
        [Service] DessertService service)
    {
        DessertDetailDto created = DessertDetailDto.From(dto);
        return service.Add(created);
    }

    [Authorize(Policy = "perm:desserts:update")]
    public DessertDetailDto? UpdateDessert(
        int id,
        DessertDtoInput dto,
        [Service] DessertService service)
    {
        DessertDetailDto detail = DessertDetailDto.From(dto);
        return service.Update(id, detail);
    }

    [Authorize(Policy = "perm:desserts:delete")]
    public bool DeleteDessert(
        int id,
        [Service] DessertService service)
    {
        return service.Delete(id);
    }

    [Authorize(Roles = new[] { "Admin" })]
    public bool SyncDesserts(
        List<SyncOperation> operations,
        [Service] DessertService service)
    {
        foreach (var op in operations)
        {
            switch (op.Type)
            {
                case "CREATE" when op.Payload != null:
                    service.Add(op.Payload);
                    break;

                case "UPDATE" when op.Payload != null && op.TargetId != null:
                    service.Update(op.TargetId.Value, op.Payload);
                    break;

                case "DELETE" when op.TargetId != null:
                    service.Delete(op.TargetId.Value);
                    break;
            }
        }

        return true;
    }


    //orders

    // Customers can place an order. They are NOT allowed to specify a userId
    // other than their own — the resolver overrides the input's UserId with
    // the caller's id so customer A can't impersonate customer B at the API
    // boundary. Admin keeps the explicit userId since admins do place orders
    // on behalf of customers (back-office UX).
    [Authorize(Policy = "perm:orders:create")]
    public async Task<CreateOrderPayload> CreateOrder(
        CreateOrderDtoInput dtoInput,
        [Service] OrderService service,
        [Service] IHttpContextAccessor http,
        [Service] IUserBlocklist blocklist,
        CancellationToken ct)
    {
        GraphQLAuthContext.Caller caller = GraphQLAuthContext.From(http);
        if (!caller.IsAdmin && caller.UserId is int callerId)
        {
            dtoInput.UserId = callerId;
        }

        BlockStatus status = await blocklist.CheckAsync(
            $"user:{dtoInput.UserId}", OrderBurstRule.BlockReasonCode, ct);
        if (status.IsBlocked)
        {
            // Return the block info as part of the payload rather than
            // throwing. Frontend reads payload.blockedMessage / order to
            // decide what to show — no GraphQL error path, no exception
            // serialization brittleness.
            return new CreateOrderPayload
            {
                BlockedMessage =
                    $"You are temporarily blocked from placing orders. Try again in {status.RetryAfterSeconds}s.",
                RetryAfterSeconds = status.RetryAfterSeconds,
            };
        }

        OrderDto created = service.Create(dtoInput);
        return new CreateOrderPayload { Order = created };
    }

    // Mirrors the GetOrderById pattern: any authenticated user may attempt,
    // resolver enforces "admin OR own order" inline. Avoids a static policy
    // that would deny based on a stale `permissions` claim from a pre-seed
    // JWT — once the principal is authenticated, the ownership rule alone
    // decides.
    [Authorize]
    public bool DeleteOrder(
        int id,
        [Service] OrderService service,
        [Service] IHttpContextAccessor http)
    {
        GraphQLAuthContext.Caller caller = GraphQLAuthContext.From(http);
        OrderDto? order = service.GetById(id);
        if (order == null) return false;

        bool isOwner = caller.UserId is int callerId && order.User.Id == callerId;
        if (!caller.IsAdmin && !isOwner)
        {
            throw new GraphQLException("You can only delete your own orders.");
        }

        return service.Delete(id);
    }

    [Authorize(Policy = "perm:orders:archive")]
    public bool ArchiveOrder(
        int id,
        [Service] OrderService service)
    {
        return service.Archive(id);
    }
}

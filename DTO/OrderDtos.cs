namespace AspireApp1.Server.DTO
{
    public class OrderDto
    {
        public int Id { get; set; }
        public int DessertId { get; set; }
        public string DessertName { get; set; } = "";
        public string Quantity { get; set; } = "";
        public OrderUserDto User { get; set; } = new();
        public DateTime OrderedAt { get; set; }
        public bool Archived { get; set; }
    }

    public class OrderUserDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string Surname { get; set; } = "";
    }

    public class CreateOrderDtoInput
    {
        public int DessertId { get; set; }
        public int UserId { get; set; }
    }

    // GraphQL return type for CreateOrder. The mutation never throws when the
    // user is rate-limited — it returns this payload with BlockedMessage set
    // instead. Clients check Order vs BlockedMessage to branch their UI.
    public class CreateOrderPayload
    {
        public OrderDto? Order { get; set; }
        public string? BlockedMessage { get; set; }
        public int? RetryAfterSeconds { get; set; }
    }
}


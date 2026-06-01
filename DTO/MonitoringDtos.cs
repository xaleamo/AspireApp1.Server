namespace AspireApp1.Server.DTO
{
    public class ActionLogDto
    {
        public long Id { get; set; }
        public int? UserId { get; set; }
        public string? UserEmail { get; set; }
        public int? RoleId { get; set; }
        public string? RoleName { get; set; }
        public string ActionType { get; set; } = "";
        public string? EntityType { get; set; }
        public string? EntityId { get; set; }
        public string? Details { get; set; }
        public bool Success { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class PagedActionLogDto
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public List<ActionLogDto> Items { get; set; } = new();
    }

    public class MonitoredUserDto
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string? UserEmail { get; set; }
        public string Identifier { get; set; } = "";
        public string Reason { get; set; } = "";
        public DateTime FlaggedAt { get; set; }
        public DateTime WindowStart { get; set; }
        public int HitCount { get; set; }
        public bool Resolved { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public int? ResolvedByUserId { get; set; }
    }
}

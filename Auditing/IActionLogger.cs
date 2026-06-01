namespace AspireApp1.Server.Auditing
{
    public interface IActionLogger
    {
        Task LogAsync(
            string actionType,
            string? entityType = null,
            string? entityId = null,
            string? details = null,
            bool success = true,
            int? overrideUserId = null,
            int? overrideRoleId = null,
            CancellationToken ct = default);
    }
}

using AspireApp1.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace AspireApp1.Server.Auditing
{
    public class ActionLogger : IActionLogger
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly ICurrentUserContext _currentUser;
        private readonly ILogger<ActionLogger> _logger;

        public ActionLogger(
            IDbContextFactory<AppDbContext> dbFactory,
            ICurrentUserContext currentUser,
            ILogger<ActionLogger> logger)
        {
            _dbFactory = dbFactory;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task LogAsync(
            string actionType,
            string? entityType = null,
            string? entityId = null,
            string? details = null,
            bool success = true,
            int? overrideUserId = null,
            int? overrideRoleId = null,
            CancellationToken ct = default)
        {
            (int? ctxUserId, int? ctxRoleId) = await _currentUser.GetAsync(ct);

            ActionLog entry = new()
            {
                UserId = overrideUserId ?? ctxUserId,
                RoleId = overrideRoleId ?? ctxRoleId,
                ActionType = actionType,
                EntityType = entityType,
                EntityId = entityId,
                Details = Truncate(details, 1000),
                Success = success,
                Timestamp = DateTime.UtcNow,
            };

            try
            {
                // Use a short-lived, dedicated context so concurrent writes from
                // fire-and-forget calls in repository decorators never collide
                // with the request-scoped DbContext.
                await using AppDbContext db = await _dbFactory.CreateDbContextAsync(ct);
                db.ActionLogs.Add(entry);
                await db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to persist ActionLog entry (ActionType={ActionType})",
                    actionType);
            }
        }

        private static string? Truncate(string? value, int max)
        {
            if (value == null) return null;
            return value.Length <= max ? value : value[..max];
        }
    }
}

using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace AspireApp1.Server.Auditing
{
    public class CurrentUserContext : ICurrentUserContext
    {
        // Legacy header — still honored as a fallback during the JWT cutover so
        // existing audit code does not log null users while older callers exist.
        public const string UserIdHeader = "X-User-Id";

        private readonly IHttpContextAccessor _accessor;
        private readonly IDbContextFactory<AppDbContext> _dbFactory;

        private bool _resolved;
        private int? _userId;
        private int? _roleId;

        public CurrentUserContext(
            IHttpContextAccessor accessor,
            IDbContextFactory<AppDbContext> dbFactory)
        {
            _accessor = accessor;
            _dbFactory = dbFactory;
        }

        public async Task<(int? UserId, int? RoleId)> GetAsync(CancellationToken ct = default)
        {
            if (_resolved) return (_userId, _roleId);

            HttpContext? http = _accessor.HttpContext;
            int? userId = TryGetFromClaims(http) ?? TryGetFromHeader(http);

            if (userId != null)
            {
                await using AppDbContext db = await _dbFactory.CreateDbContextAsync(ct);
                int? roleId = await db.Users
                    .Where(u => u.Id == userId)
                    .Select(u => (int?)u.RoleId)
                    .FirstOrDefaultAsync(ct);

                if (roleId != null)
                {
                    _userId = userId;
                    _roleId = roleId;
                }
            }

            _resolved = true;
            return (_userId, _roleId);
        }

        private static int? TryGetFromClaims(HttpContext? http)
        {
            ClaimsPrincipal? principal = http?.User;
            if (principal?.Identity?.IsAuthenticated != true) return null;
            string? sub = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                          ?? principal.FindFirstValue("sub");
            return int.TryParse(sub, out int id) ? id : null;
        }

        private static int? TryGetFromHeader(HttpContext? http)
        {
            if (http == null) return null;
            if (!http.Request.Headers.TryGetValue(UserIdHeader, out Microsoft.Extensions.Primitives.StringValues raw))
                return null;
            return int.TryParse(raw.ToString(), out int id) ? id : null;
        }
    }
}

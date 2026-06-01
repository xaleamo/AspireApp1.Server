using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AspireApp1.Server.Security
{
    // Resolves "is this identifier (email) currently in the active-block window?"
    // by consulting MonitoredUsers for an unresolved LoginBlocked row whose
    // FlaggedAt is within LoginBlockMinutes of "now". Lives behind a short-lived
    // DbContext from the factory so background callers and request callers don't
    // collide on a shared scoped context.
    public class UserBlocklist : IUserBlocklist
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly SecurityOptions _opts;

        public UserBlocklist(
            IDbContextFactory<AppDbContext> dbFactory,
            IOptions<SecurityOptions> opts)
        {
            _dbFactory = dbFactory;
            _opts = opts.Value;
        }

        public async Task<BlockStatus> CheckAsync(string identifier, string reasonCode, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(identifier)) return BlockStatus.NotBlocked;

            DateTime now = DateTime.UtcNow;
            DateTime cutoff = now.AddMinutes(-_opts.LoginBlockMinutes);

            await using AppDbContext db = await _dbFactory.CreateDbContextAsync(ct);
            DateTime? flaggedAt = await db.MonitoredUsers
                .Where(m => m.Identifier == identifier
                            && m.Reason == reasonCode
                            && !m.Resolved
                            && m.FlaggedAt >= cutoff)
                .OrderByDescending(m => m.FlaggedAt)
                .Select(m => (DateTime?)m.FlaggedAt)
                .FirstOrDefaultAsync(ct);

            if (flaggedAt == null) return BlockStatus.NotBlocked;

            DateTime unblockAt = flaggedAt.Value.AddMinutes(_opts.LoginBlockMinutes);
            return new BlockStatus(true, unblockAt);
        }
    }
}

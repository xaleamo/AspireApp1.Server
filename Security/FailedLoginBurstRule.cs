using AspireApp1.Server.Models;
using AspireApp1.Server.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AspireApp1.Server.Security
{
    public class FailedLoginBurstRule : IThreatRule
    {
        public const string ReasonCode = "FailedLoginBurst";
        public const string BlockReasonCode = "LoginBlocked";

        // ActionTypes that count as a failed login attempt. Keep in sync with
        // AuthService.LoginFailedAction / LoginLockoutAction / MfaFailedAction.
        public static readonly string[] FailedLoginActionTypes =
        {
            AuthService.LoginFailedAction,
            AuthService.LoginLockoutAction,
            AuthService.MfaFailedAction,
        };

        // Kept for backwards-compat with old tests that referenced this constant.
        public const string LoginActionType = "Auth.Login.Failed";

        private readonly SecurityOptions _opts;

        public FailedLoginBurstRule(IOptions<SecurityOptions> opts)
        {
            _opts = opts.Value;
        }

        public string Name => ReasonCode;

        public async Task EvaluateAsync(AppDbContext db, DateTime now, CancellationToken ct)
        {
            DateTime windowStart = now.AddMinutes(-_opts.FailedLoginWindowMinutes);
            int observeThreshold = _opts.FailedLoginThreshold;
            int blockThreshold = _opts.LoginBlockThreshold;

            // Auto-resolve LoginBlocked rows whose 5-minute window has elapsed.
            // We do this each tick so the observation list never shows stale
            // blocks and IUserBlocklist's "is this email blocked right now?"
            // query stays cheap.
            DateTime blockCutoff = now.AddMinutes(-_opts.LoginBlockMinutes);
            List<MonitoredUser> expiredBlocks = await db.MonitoredUsers
                .Where(m => m.Reason == BlockReasonCode
                            && !m.Resolved
                            && m.FlaggedAt < blockCutoff)
                .ToListAsync(ct);
            foreach (MonitoredUser b in expiredBlocks)
            {
                b.Resolved = true;
                b.ResolvedAt = now;
            }

            // Pull every relevant failed-login row in one query, then group in
            // memory by extracted email (the Details field is "email=foo;reason=...")
            List<ActionLog> recent = await db.ActionLogs
                .AsNoTracking()
                .Where(l => FailedLoginActionTypes.Contains(l.ActionType)
                            && !l.Success
                            && l.Details != null
                            && l.Timestamp >= windowStart)
                .ToListAsync(ct);

            var grouped = recent
                .Select(l => new { Email = ExtractEmail(l.Details!), Log = l })
                .Where(x => !string.IsNullOrEmpty(x.Email))
                .GroupBy(x => x.Email!)
                .Select(g => new
                {
                    Identifier = g.Key,
                    Count = g.Count(),
                    FirstSeen = g.Min(x => x.Log.Timestamp),
                })
                .ToList();

            foreach (var burst in grouped)
            {
                if (burst.Count < observeThreshold) continue;

                int? userId = await db.Users
                    .Where(u => u.NormalizedEmail == burst.Identifier.ToUpperInvariant())
                    .Select(u => (int?)u.Id)
                    .FirstOrDefaultAsync(ct);

                // 5+ failures → observation entry.
                bool observationAlreadyOpen = await db.MonitoredUsers
                    .AnyAsync(m => m.Identifier == burst.Identifier
                                   && m.Reason == ReasonCode
                                   && !m.Resolved, ct);
                if (!observationAlreadyOpen)
                {
                    db.MonitoredUsers.Add(new MonitoredUser
                    {
                        UserId = userId,
                        Identifier = burst.Identifier,
                        Reason = ReasonCode,
                        FlaggedAt = now,
                        WindowStart = burst.FirstSeen,
                        HitCount = burst.Count,
                    });
                }

                // 10+ failures → hard block (separate row, separate reason).
                if (burst.Count >= blockThreshold)
                {
                    bool blockAlreadyOpen = await db.MonitoredUsers
                        .AnyAsync(m => m.Identifier == burst.Identifier
                                       && m.Reason == BlockReasonCode
                                       && !m.Resolved, ct);
                    if (!blockAlreadyOpen)
                    {
                        db.MonitoredUsers.Add(new MonitoredUser
                        {
                            UserId = userId,
                            Identifier = burst.Identifier,
                            Reason = BlockReasonCode,
                            FlaggedAt = now,
                            WindowStart = burst.FirstSeen,
                            HitCount = burst.Count,
                        });
                    }
                }
            }

            if (db.ChangeTracker.HasChanges())
            {
                await db.SaveChangesAsync(ct);
            }
        }

        // Audit details look like "email=foo@bar.com;reason=BadPassword".
        // Some older entries store the bare email. Handle both.
        public static string? ExtractEmail(string details)
        {
            if (string.IsNullOrEmpty(details)) return null;

            const string prefix = "email=";
            int start = details.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
            if (start < 0)
            {
                // Fall back to "is this whole string an email-like token?" so
                // legacy logs without the structured format still group.
                return details.Contains('@') ? details : null;
            }
            start += prefix.Length;
            int end = details.IndexOf(';', start);
            string email = end < 0 ? details[start..] : details[start..end];
            return email.Trim();
        }
    }
}

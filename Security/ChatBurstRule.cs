using AspireApp1.Server.Models;
using AspireApp1.Server.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AspireApp1.Server.Security
{
    public class ChatBurstRule : IThreatRule
    {
        public const string ReasonCode = "ChatBurst";
        public const string BlockReasonCode = "MessageBlocked";

        // Must match the action type emitted by LoggingChatRepository.AddAsync.
        public static readonly string ChatAddActionType =
            $"{nameof(ChatRepository)}.{nameof(IChatRepository.AddAsync)}";

        private readonly SecurityOptions _opts;

        public ChatBurstRule(IOptions<SecurityOptions> opts)
        {
            _opts = opts.Value;
        }

        public string Name => ReasonCode;

        public async Task EvaluateAsync(AppDbContext db, DateTime now, CancellationToken ct)
        {
            DateTime windowStart = now.AddMinutes(-_opts.ChatBurstWindowMinutes);
            int observeThreshold = _opts.ChatBurstThreshold;
            int blockThreshold = _opts.ChatBlockThreshold;

            // Auto-resolve expired block rows so the observation list never
            // shows stale entries and IUserBlocklist's lookup stays cheap.
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

            var bursts = await db.ActionLogs
                .AsNoTracking()
                .Where(l => l.ActionType == ChatAddActionType
                            && l.Success
                            && l.UserId != null
                            && l.Timestamp >= windowStart)
                .GroupBy(l => l.UserId!.Value)
                .Select(g => new
                {
                    UserId = g.Key,
                    Count = g.Count(),
                    FirstSeen = g.Min(x => x.Timestamp),
                })
                .Where(g => g.Count >= observeThreshold)
                .ToListAsync(ct);

            foreach (var burst in bursts)
            {
                string identifier = $"user:{burst.UserId}";

                bool observationAlreadyOpen = await db.MonitoredUsers
                    .AnyAsync(m => m.Identifier == identifier
                                   && m.Reason == ReasonCode
                                   && !m.Resolved, ct);
                if (!observationAlreadyOpen)
                {
                    db.MonitoredUsers.Add(new MonitoredUser
                    {
                        UserId = burst.UserId,
                        Identifier = identifier,
                        Reason = ReasonCode,
                        FlaggedAt = now,
                        WindowStart = burst.FirstSeen,
                        HitCount = burst.Count,
                    });
                }

                if (burst.Count >= blockThreshold)
                {
                    bool blockAlreadyOpen = await db.MonitoredUsers
                        .AnyAsync(m => m.Identifier == identifier
                                       && m.Reason == BlockReasonCode
                                       && !m.Resolved, ct);
                    if (!blockAlreadyOpen)
                    {
                        db.MonitoredUsers.Add(new MonitoredUser
                        {
                            UserId = burst.UserId,
                            Identifier = identifier,
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
    }
}

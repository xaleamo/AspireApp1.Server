using System.Security.Cryptography;
using AspireApp1.Server.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AspireApp1.Server.Services.Auth
{
    public class RefreshTokenStore : IRefreshTokenStore
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly JwtOptions _options;

        public RefreshTokenStore(IDbContextFactory<AppDbContext> dbFactory, IOptions<JwtOptions> options)
        {
            _dbFactory = dbFactory;
            _options = options.Value;
        }

        public async Task<IssuedRefreshToken> IssueAsync(int userId, string? remoteIp, CancellationToken ct = default)
        {
            string raw = GenerateRaw();
            string hash = Hash(raw);
            DateTime exp = DateTime.UtcNow.AddDays(_options.RefreshTokenDays);

            await using AppDbContext db = await _dbFactory.CreateDbContextAsync(ct);
            db.RefreshTokens.Add(new RefreshToken
            {
                UserId = userId,
                TokenHash = hash,
                ExpiresAt = exp,
                CreatedAt = DateTime.UtcNow,
                RemoteIp = remoteIp,
            });
            await db.SaveChangesAsync(ct);

            return new IssuedRefreshToken(raw, exp);
        }

        public async Task<RefreshOutcome> RotateAsync(string rawToken, string? remoteIp, CancellationToken ct = default)
        {
            string hash = Hash(rawToken);
            await using AppDbContext db = await _dbFactory.CreateDbContextAsync(ct);

            RefreshToken? entry = await db.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.TokenHash == hash, ct);

            if (entry == null) return new RefreshOutcome(RefreshResult.NotFound, null, null, null);

            if (entry.RevokedAt != null)
            {
                // Reuse of a previously-rotated token: revoke the entire family.
                await RevokeAllForUserInternalAsync(db, entry.UserId, "refresh_token_reuse", ct);
                return new RefreshOutcome(RefreshResult.Reuse, entry.UserId, null, null);
            }

            if (DateTime.UtcNow >= entry.ExpiresAt)
            {
                return new RefreshOutcome(RefreshResult.Expired, entry.UserId, null, null);
            }

            string newRaw = GenerateRaw();
            string newHash = Hash(newRaw);
            DateTime newExp = DateTime.UtcNow.AddDays(_options.RefreshTokenDays);

            entry.RevokedAt = DateTime.UtcNow;
            entry.ReasonRevoked = "rotated";
            entry.ReplacedByTokenHash = newHash;

            db.RefreshTokens.Add(new RefreshToken
            {
                UserId = entry.UserId,
                TokenHash = newHash,
                ExpiresAt = newExp,
                CreatedAt = DateTime.UtcNow,
                RemoteIp = remoteIp,
            });
            await db.SaveChangesAsync(ct);

            return new RefreshOutcome(RefreshResult.Ok, entry.UserId, newRaw, newExp);
        }

        public async Task RevokeAsync(string rawToken, string reason, CancellationToken ct = default)
        {
            string hash = Hash(rawToken);
            await using AppDbContext db = await _dbFactory.CreateDbContextAsync(ct);

            RefreshToken? entry = await db.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.TokenHash == hash, ct);

            if (entry == null || entry.RevokedAt != null) return;

            entry.RevokedAt = DateTime.UtcNow;
            entry.ReasonRevoked = reason;
            await db.SaveChangesAsync(ct);
        }

        public async Task RevokeAllForUserAsync(int userId, string reason, CancellationToken ct = default)
        {
            await using AppDbContext db = await _dbFactory.CreateDbContextAsync(ct);
            await RevokeAllForUserInternalAsync(db, userId, reason, ct);
        }

        private static async Task RevokeAllForUserInternalAsync(AppDbContext db, int userId, string reason, CancellationToken ct)
        {
            List<RefreshToken> active = await db.RefreshTokens
                .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
                .ToListAsync(ct);

            DateTime now = DateTime.UtcNow;
            foreach (RefreshToken t in active)
            {
                t.RevokedAt = now;
                t.ReasonRevoked = reason;
            }
            await db.SaveChangesAsync(ct);
        }

        private static string GenerateRaw()
        {
            Span<byte> buf = stackalloc byte[64];
            RandomNumberGenerator.Fill(buf);
            return Convert.ToBase64String(buf);
        }

        private static string Hash(string raw)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(raw);
            byte[] digest = SHA256.HashData(bytes);
            return Convert.ToHexString(digest);
        }
    }
}

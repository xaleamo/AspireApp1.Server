namespace AspireApp1.Server.Services.Auth
{
    public record IssuedRefreshToken(string RawToken, DateTime ExpiresAt);

    public enum RefreshResult
    {
        Ok,
        NotFound,
        Expired,
        Revoked,
        Reuse,
    }

    public record RefreshOutcome(RefreshResult Result, int? UserId, string? NewRawToken, DateTime? NewExpiresAt);

    public interface IRefreshTokenStore
    {
        Task<IssuedRefreshToken> IssueAsync(int userId, string? remoteIp, CancellationToken ct = default);
        Task<RefreshOutcome> RotateAsync(string rawToken, string? remoteIp, CancellationToken ct = default);
        Task RevokeAsync(string rawToken, string reason, CancellationToken ct = default);
        Task RevokeAllForUserAsync(int userId, string reason, CancellationToken ct = default);
    }
}

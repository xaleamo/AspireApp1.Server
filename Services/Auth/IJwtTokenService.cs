using AspireApp1.Server.Models;

namespace AspireApp1.Server.Services.Auth
{
    public record IssuedAccessToken(string Token, DateTime ExpiresAt, string Jti);

    public record IssuedMfaChallenge(string Token, DateTime ExpiresAt);

    public record MfaChallengePayload(int UserId, IReadOnlyList<string> RemainingFactors);

    public interface IJwtTokenService
    {
        IssuedAccessToken IssueAccessToken(User user, string roleName, IReadOnlyCollection<string> permissions);
        IssuedMfaChallenge IssueMfaChallengeToken(User user, IEnumerable<string> remainingFactors);
        MfaChallengePayload? ValidateMfaChallengeToken(string token);
    }
}

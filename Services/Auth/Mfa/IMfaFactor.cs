using AspireApp1.Server.Models;

namespace AspireApp1.Server.Services.Auth.Mfa
{
    public interface IMfaFactor
    {
        string Name { get; }

        Task<bool> IsEnabledForUserAsync(User user, CancellationToken ct = default);

        Task SendChallengeAsync(User user, CancellationToken ct = default);

        Task<bool> VerifyAsync(User user, string code, CancellationToken ct = default);
    }
}

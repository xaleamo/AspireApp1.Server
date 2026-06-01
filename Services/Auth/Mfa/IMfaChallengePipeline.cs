using AspireApp1.Server.Models;

namespace AspireApp1.Server.Services.Auth.Mfa
{
    public record MfaFactorStatus(string Name, bool Enabled);

    public interface IMfaChallengePipeline
    {
        Task<IReadOnlyList<string>> GetEnabledFactorNamesAsync(User user, CancellationToken ct = default);

        Task<IReadOnlyList<MfaFactorStatus>> GetAllFactorStatusAsync(User user, CancellationToken ct = default);

        Task<bool> AnyEnabledAsync(User user, CancellationToken ct = default);

        Task SendChallengeAsync(User user, string factorName, CancellationToken ct = default);

        Task<bool> VerifyAsync(User user, string factorName, string code, CancellationToken ct = default);

        IMfaFactor? GetFactor(string factorName);
    }
}

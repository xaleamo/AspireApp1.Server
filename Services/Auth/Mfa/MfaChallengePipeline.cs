using AspireApp1.Server.Models;

namespace AspireApp1.Server.Services.Auth.Mfa
{
    public class MfaChallengePipeline : IMfaChallengePipeline
    {
        private readonly Dictionary<string, IMfaFactor> _factors;

        public MfaChallengePipeline(IEnumerable<IMfaFactor> factors)
        {
            _factors = factors.ToDictionary(f => f.Name, StringComparer.OrdinalIgnoreCase);
        }

        public async Task<IReadOnlyList<string>> GetEnabledFactorNamesAsync(User user, CancellationToken ct = default)
        {
            var enabled = new List<string>();
            foreach (IMfaFactor f in _factors.Values)
            {
                if (await f.IsEnabledForUserAsync(user, ct))
                {
                    enabled.Add(f.Name);
                }
            }
            return enabled;
        }

        public async Task<IReadOnlyList<MfaFactorStatus>> GetAllFactorStatusAsync(User user, CancellationToken ct = default)
        {
            var statuses = new List<MfaFactorStatus>();
            foreach (IMfaFactor f in _factors.Values)
            {
                bool on = await f.IsEnabledForUserAsync(user, ct);
                statuses.Add(new MfaFactorStatus(f.Name, on));
            }
            return statuses;
        }

        public async Task<bool> AnyEnabledAsync(User user, CancellationToken ct = default)
        {
            foreach (IMfaFactor f in _factors.Values)
            {
                if (await f.IsEnabledForUserAsync(user, ct)) return true;
            }
            return false;
        }

        public async Task SendChallengeAsync(User user, string factorName, CancellationToken ct = default)
        {
            if (!_factors.TryGetValue(factorName, out IMfaFactor? f))
                throw new InvalidOperationException($"Unknown MFA factor '{factorName}'.");
            await f.SendChallengeAsync(user, ct);
        }

        public async Task<bool> VerifyAsync(User user, string factorName, string code, CancellationToken ct = default)
        {
            if (!_factors.TryGetValue(factorName, out IMfaFactor? f)) return false;
            return await f.VerifyAsync(user, code, ct);
        }

        public IMfaFactor? GetFactor(string factorName) =>
            _factors.TryGetValue(factorName, out IMfaFactor? f) ? f : null;
    }
}

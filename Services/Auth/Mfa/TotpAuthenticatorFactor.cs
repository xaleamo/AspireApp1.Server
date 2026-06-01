using AspireApp1.Server.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AspireApp1.Server.Services.Auth.Mfa
{
    public class TotpAuthenticatorFactor : IMfaFactor
    {
        public const string FactorName = "Authenticator";

        private readonly UserManager<User> _userManager;
        private readonly IDbContextFactory<AppDbContext> _dbFactory;

        public TotpAuthenticatorFactor(
            UserManager<User> userManager,
            IDbContextFactory<AppDbContext> dbFactory)
        {
            _userManager = userManager;
            _dbFactory = dbFactory;
        }

        public string Name => FactorName;

        public async Task<bool> IsEnabledForUserAsync(User user, CancellationToken ct = default)
        {
            await using AppDbContext db = await _dbFactory.CreateDbContextAsync(ct);
            return await db.UserMfaFactors
                .AnyAsync(factor => factor.UserId == user.Id && factor.FactorName == FactorName, ct);
        }

        public Task SendChallengeAsync(User user, CancellationToken ct = default)
        {
            // Authenticator apps generate codes locally — nothing to send.
            return Task.CompletedTask;
        }

        public async Task<bool> VerifyAsync(User user, string code, CancellationToken ct = default)
        {
            return await _userManager.VerifyTwoFactorTokenAsync(
                user,
                _userManager.Options.Tokens.AuthenticatorTokenProvider,
                code);
        }
    }
}

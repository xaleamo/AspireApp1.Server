using AspireApp1.Server.Models;
using AspireApp1.Server.Services.Email;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AspireApp1.Server.Services.Auth.Mfa
{
    public class EmailOtpFactor : IMfaFactor
    {
        public const string FactorName = "Email";

        // Identity's built-in EmailTokenProvider key.
        private const string TokenProvider = "Email";
        private const string TokenPurpose = "TwoFactor";

        private readonly UserManager<User> _userManager;
        private readonly IEmailSender _email;
        private readonly IDbContextFactory<AppDbContext> _dbFactory;

        public EmailOtpFactor(
            UserManager<User> userManager,
            IEmailSender email,
            IDbContextFactory<AppDbContext> dbFactory)
        {
            _userManager = userManager;
            _email = email;
            _dbFactory = dbFactory;
        }

        public string Name => FactorName;

        public async Task<bool> IsEnabledForUserAsync(User user, CancellationToken ct = default)
        {
            await using AppDbContext db = await _dbFactory.CreateDbContextAsync(ct);
            return await db.UserMfaFactors
                .AnyAsync(f => f.UserId == user.Id && f.FactorName == FactorName, ct);
        }

        public async Task SendChallengeAsync(User user, CancellationToken ct = default)
        {
            string code = await _userManager.GenerateUserTokenAsync(user, TokenProvider, TokenPurpose);
            await _email.SendAsync(
                to: user.Email ?? throw new InvalidOperationException("User has no email."),
                subject: "Your login verification code",
                body: $"Your verification code is: {code}\nIt expires in a few minutes.",
                ct);
        }

        public async Task<bool> VerifyAsync(User user, string code, CancellationToken ct = default)
        {
            return await _userManager.VerifyUserTokenAsync(user, TokenProvider, TokenPurpose, code);
        }
    }
}

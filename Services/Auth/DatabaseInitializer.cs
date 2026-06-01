using AspireApp1.Server.Models;
using AspireApp1.Server.Services.Auth.Mfa;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AspireApp1.Server.Services.Auth
{
    public static class DatabaseInitializer
    {
        public const string AdminRoleName = "Admin";
        public const string CustomerRoleName = "Customer";

        public static async Task MigrateAndSeedAsync(IServiceProvider services)
        {
            using IServiceScope scope = services.CreateScope();
            IServiceProvider sp = scope.ServiceProvider;

            AppDbContext db = sp.GetRequiredService<AppDbContext>();
            await db.Database.MigrateAsync();

            UserManager<User> userManager = sp.GetRequiredService<UserManager<User>>();
            RoleManager<Role> roleManager = sp.GetRequiredService<RoleManager<Role>>();

            await EnsureRoleAsync(roleManager, AdminRoleName);
            await EnsureRoleAsync(roleManager, CustomerRoleName);

            // Backfill Identity columns for users that were inserted by the
            // pre-Identity register flow. Their NormalizedEmail / UserName /
            // NormalizedUserName / SecurityStamp / ConcurrencyStamp are NULL,
            // so UserManager.FindByEmailAsync can't see them and password reset
            // appears to silently do nothing.
            await BackfillIdentityFieldsAsync(db, userManager);

            await EnsureUserAsync(userManager, db,
                email: "xaleamo@gmail.com",
                password: "admin123",
                firstName: "Admin",
                surname: "Razlog",
                roleName: AdminRoleName);

            await EnsureUserAsync(userManager, db,
                email: "customer@gmail.com",
                password: "customer123",
                firstName: "John",
                surname: "Doe",
                roleName: CustomerRoleName);

            // Pre-enable both MFA factors for the admin so the full 3FA flow
            // (password → email OTP → TOTP) can be demonstrated manually.
            ILoggerFactory loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            ILogger logger = loggerFactory.CreateLogger("DatabaseInitializer");
            await EnsureAdminMfaAsync(db, userManager, logger);
        }

        private static async Task EnsureAdminMfaAsync(AppDbContext db, UserManager<User> userManager, ILogger logger)
        {
            User? admin = await userManager.FindByEmailAsync("xaleamo@gmail.com");
            if (admin == null) return;

            bool emailEnabled = await db.UserMfaFactors
                .AnyAsync(f => f.UserId == admin.Id && f.FactorName == EmailOtpFactor.FactorName);
            bool authEnabled = await db.UserMfaFactors
                .AnyAsync(f => f.UserId == admin.Id && f.FactorName == TotpAuthenticatorFactor.FactorName);

            if (!emailEnabled)
            {
                db.UserMfaFactors.Add(new UserMfaFactor
                {
                    UserId = admin.Id,
                    FactorName = EmailOtpFactor.FactorName,
                });
            }
            if (!authEnabled)
            {
                db.UserMfaFactors.Add(new UserMfaFactor
                {
                    UserId = admin.Id,
                    FactorName = TotpAuthenticatorFactor.FactorName,
                });
            }
            if (!emailEnabled || !authEnabled)
            {
                await db.SaveChangesAsync();
            }

            if (!admin.TwoFactorEnabled)
            {
                admin.TwoFactorEnabled = true;
                await userManager.UpdateAsync(admin);
            }

            // TOTP shared key is stored in AspNetUserTokens by Identity's
            // authenticator token provider. Generate it once and log the
            // otpauth URI + manual key so the developer can scan/type it into
            // Microsoft Authenticator. Without this, the seeded key is unusable
            // because the phone has no copy of it.
            string? key = await userManager.GetAuthenticatorKeyAsync(admin);
            bool keyWasGenerated = false;
            if (string.IsNullOrEmpty(key))
            {
                await userManager.ResetAuthenticatorKeyAsync(admin);
                key = await userManager.GetAuthenticatorKeyAsync(admin);
                keyWasGenerated = true;
            }

            string uri = TotpUri.Build(TotpUri.Issuer, admin.Email ?? "admin", key ?? "");
            string prefix = keyWasGenerated ? "(new key generated)" : "(existing key)";
            logger.LogInformation(
                "\n========== ADMIN MFA SETUP (dev) {Prefix} ==========\n" +
                "Account:     {Email}\n" +
                "Factors:     Email + Authenticator (both enabled)\n" +
                "\n" +
                "Manual setup in Microsoft / Google Authenticator:\n" +
                "  1. Add account -> Other -> 'Enter code manually'\n" +
                "  2. Account name: anything you like\n" +
                "  3. Secret key:   {Key}\n" +
                "  (paste ONLY the key above; do NOT paste the full URI)\n" +
                "\n" +
                "QR-scan workflow (encode this URI as a QR code):\n" +
                "  {Uri}\n" +
                "===================================================\n",
                prefix, admin.Email, key, uri);
        }

        private static async Task BackfillIdentityFieldsAsync(AppDbContext db, UserManager<User> userManager)
        {
            // Filter both columns nullable-aware — EF translates `== null` to
            // SQL `IS NULL` correctly with PostgreSQL.
            List<User> stale = await db.Users
                .Where(u => u.NormalizedEmail == null || u.SecurityStamp == null || u.UserName == null)
                .ToListAsync();

            if (stale.Count == 0) return;

            foreach (User u in stale)
            {
                if (string.IsNullOrEmpty(u.UserName))
                {
                    u.UserName = u.Email;
                }
                if (string.IsNullOrEmpty(u.NormalizedUserName) && !string.IsNullOrEmpty(u.UserName))
                {
                    u.NormalizedUserName = userManager.NormalizeName(u.UserName);
                }
                if (string.IsNullOrEmpty(u.NormalizedEmail) && !string.IsNullOrEmpty(u.Email))
                {
                    u.NormalizedEmail = userManager.NormalizeEmail(u.Email);
                }
                if (string.IsNullOrEmpty(u.SecurityStamp))
                {
                    u.SecurityStamp = Guid.NewGuid().ToString("N");
                }
                if (string.IsNullOrEmpty(u.ConcurrencyStamp))
                {
                    u.ConcurrencyStamp = Guid.NewGuid().ToString();
                }
            }
            await db.SaveChangesAsync();
        }

        private static async Task EnsureRoleAsync(RoleManager<Role> roleManager, string name)
        {
            Role? existing = await roleManager.FindByNameAsync(name);
            if (existing == null)
            {
                await roleManager.CreateAsync(new Role(name));
            }
        }

        private static async Task EnsureUserAsync(
            UserManager<User> userManager,
            AppDbContext db,
            string email,
            string password,
            string firstName,
            string surname,
            string roleName)
        {
            Role? role = await db.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
            if (role == null) return;

            User? existing = await userManager.FindByEmailAsync(email);
            if (existing != null)
            {
                // Make seed idempotent: any prior run that used a different
                // password (e.g. earlier seed run with stricter policy) gets
                // reset back to the canonical demo password so the developer
                // never has to wonder which one is live.
                if (!await userManager.CheckPasswordAsync(existing, password))
                {
                    string resetToken = await userManager.GeneratePasswordResetTokenAsync(existing);
                    await userManager.ResetPasswordAsync(existing, resetToken, password);
                }
                if (!await userManager.IsInRoleAsync(existing, roleName))
                {
                    await userManager.AddToRoleAsync(existing, roleName);
                }
                return;
            }

            var user = new User
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = firstName,
                Surname = surname,
                RoleId = role.Id,
            };
            IdentityResult result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to seed user {email}: {string.Join("; ", result.Errors.Select(e => e.Description))}");
            }
            await userManager.AddToRoleAsync(user, roleName);
        }
    }
}

using AspireApp1.Server.Auditing;
using AspireApp1.Server.DTO;
using AspireApp1.Server.Models;
using AspireApp1.Server.Security;
using AspireApp1.Server.Services.Auth;
using AspireApp1.Server.Services.Auth.Mfa;
using AspireApp1.Server.Services.Email;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AspireApp1.Server.Services
{
    public enum LoginOutcome
    {
        InvalidCredentials,
        LockedOut,
        Blocked,
        MfaRequired,
        EmailNotConfirmed,
        Ok,
    }

    public enum RegisterOutcome
    {
        Ok,
        EmailAlreadyExists,
        DefaultRoleMissing,
        InvalidPassword,
    }

    public enum RefreshOutcomeResult
    {
        Ok,
        Invalid,
        Reuse,
    }

    public class LoginAttemptResult
    {
        public LoginOutcome Outcome { get; init; }
        public LoginResponse? Response { get; init; }
        public string? ErrorMessage { get; init; }
        public int? UserId { get; init; }
        public int? RetryAfterSeconds { get; init; }
    }

    public class RegisterAttemptResult
    {
        public RegisterOutcome Outcome { get; init; }
        public AuthUserDto? User { get; init; }
        public string? ErrorMessage { get; init; }
    }

    public class RefreshAttemptResult
    {
        public RefreshOutcomeResult Outcome { get; init; }
        public LoginResponse? Response { get; init; }
    }

    public class ResetPasswordResult
    {
        public bool Succeeded { get; init; }
        public string? ErrorMessage { get; init; }

        public static ResetPasswordResult Success() => new() { Succeeded = true };
        public static ResetPasswordResult Fail(string message) => new() { Succeeded = false, ErrorMessage = message };
    }

    public class AuthService
    {
        public const string LoginSuccessAction = "Auth.Login.Success";
        public const string LoginFailedAction = "Auth.Login.Failed";
        public const string LoginLockoutAction = "Auth.Login.Lockout";
        public const string LoginMfaChallengeAction = "Auth.Mfa.ChallengeIssued";
        public const string MfaVerifiedAction = "Auth.Mfa.Verified";
        public const string MfaFailedAction = "Auth.Mfa.Failed";
        public const string MfaEnabledAction = "Auth.Mfa.Enabled";
        public const string MfaDisabledAction = "Auth.Mfa.Disabled";
        public const string RegisterAction = "Auth.Register";
        public const string LogoutAction = "Auth.Logout";
        public const string RefreshAction = "Auth.Refresh";
        public const string RefreshReuseAction = "Auth.Refresh.Reuse";
        public const string PasswordResetRequestedAction = "Auth.Password.ResetRequested";
        public const string PasswordResetCompletedAction = "Auth.Password.ResetCompleted";
        public const string EmailConfirmedAction = "Auth.Email.Confirmed";

        private const string DefaultCustomerRole = "Customer";

        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly IJwtTokenService _jwt;
        private readonly IRefreshTokenStore _refresh;
        private readonly IMfaChallengePipeline _mfa;
        private readonly IEmailSender _email;
        private readonly IActionLogger _log;
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly IUserBlocklist _blocklist;

        public AuthService(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            RoleManager<Role> roleManager,
            IJwtTokenService jwt,
            IRefreshTokenStore refresh,
            IMfaChallengePipeline mfa,
            IEmailSender email,
            IActionLogger log,
            IDbContextFactory<AppDbContext> dbFactory,
            IUserBlocklist blocklist)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _jwt = jwt;
            _refresh = refresh;
            _mfa = mfa;
            _email = email;
            _log = log;
            _dbFactory = dbFactory;
            _blocklist = blocklist;
        }

        public async Task<LoginAttemptResult> LoginAsync(LoginRequest request, string? remoteIp, CancellationToken ct = default)
        {
            // Block check up front so a hot-looped attacker can't even reach the
            // password hasher once 10 failures inside the window have happened.
            BlockStatus block = await _blocklist.CheckAsync(request.Email, FailedLoginBurstRule.BlockReasonCode, ct);
            if (block.IsBlocked)
            {
                await _log.LogAsync(LoginFailedAction, nameof(User), null,
                    $"email={request.Email};reason=Blocked", success: false, ct: ct);
                return new LoginAttemptResult
                {
                    Outcome = LoginOutcome.Blocked,
                    RetryAfterSeconds = block.RetryAfterSeconds,
                };
            }

            User? user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                await _log.LogAsync(LoginFailedAction, nameof(User), null,
                    $"email={request.Email};reason=NotFound", success: false, ct: ct);
                return new LoginAttemptResult { Outcome = LoginOutcome.InvalidCredentials };
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                await _log.LogAsync(LoginLockoutAction, nameof(User), user.Id.ToString(),
                    $"email={request.Email}", success: false,
                    overrideUserId: user.Id, overrideRoleId: user.RoleId, ct: ct);
                return new LoginAttemptResult { Outcome = LoginOutcome.LockedOut, UserId = user.Id };
            }

            Microsoft.AspNetCore.Identity.SignInResult result =
                await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

            if (result.IsLockedOut)
            {
                await _log.LogAsync(LoginLockoutAction, nameof(User), user.Id.ToString(),
                    $"email={request.Email}", success: false,
                    overrideUserId: user.Id, overrideRoleId: user.RoleId, ct: ct);
                return new LoginAttemptResult { Outcome = LoginOutcome.LockedOut, UserId = user.Id };
            }

            if (result.IsNotAllowed)
            {
                // Identity blocks sign-in here when RequireConfirmedEmail is on
                // and the user hasn't confirmed yet. Surface it as a distinct
                // outcome so the UI can prompt for the confirmation token
                // rather than telling them their password was wrong.
                await _log.LogAsync(LoginFailedAction, nameof(User), user.Id.ToString(),
                    $"email={request.Email};reason=EmailNotConfirmed", success: false,
                    overrideUserId: user.Id, overrideRoleId: user.RoleId, ct: ct);
                return new LoginAttemptResult { Outcome = LoginOutcome.EmailNotConfirmed, UserId = user.Id };
            }

            if (!result.Succeeded)
            {
                await _log.LogAsync(LoginFailedAction, nameof(User), user.Id.ToString(),
                    $"email={request.Email};reason=BadPassword", success: false,
                    overrideUserId: user.Id, overrideRoleId: user.RoleId, ct: ct);
                return new LoginAttemptResult { Outcome = LoginOutcome.InvalidCredentials, UserId = user.Id };
            }

            IReadOnlyList<string> enabledFactors = await _mfa.GetEnabledFactorNamesAsync(user, ct);
            if (enabledFactors.Count > 0)
            {
                IssuedMfaChallenge challenge = _jwt.IssueMfaChallengeToken(user, enabledFactors);
                await _log.LogAsync(LoginMfaChallengeAction, nameof(User), user.Id.ToString(),
                    $"email={request.Email};factors={string.Join(",", enabledFactors)}",
                    success: true, overrideUserId: user.Id, overrideRoleId: user.RoleId, ct: ct);

                return new LoginAttemptResult
                {
                    Outcome = LoginOutcome.MfaRequired,
                    UserId = user.Id,
                    Response = new LoginResponse
                    {
                        RequiresMfa = true,
                        MfaChallengeToken = challenge.Token,
                        AvailableFactors = enabledFactors.ToList(),
                    },
                };
            }

            LoginResponse ok = await BuildTokensAsync(user, remoteIp, ct);
            await _log.LogAsync(LoginSuccessAction, nameof(User), user.Id.ToString(),
                $"email={request.Email}", success: true,
                overrideUserId: user.Id, overrideRoleId: user.RoleId, ct: ct);
            return new LoginAttemptResult { Outcome = LoginOutcome.Ok, Response = ok, UserId = user.Id };
        }

        public async Task<LoginAttemptResult> VerifyMfaAsync(MfaLoginRequest request, string? remoteIp, CancellationToken ct = default)
        {
            MfaChallengePayload? payload = _jwt.ValidateMfaChallengeToken(request.MfaChallengeToken);
            if (payload == null)
            {
                return new LoginAttemptResult { Outcome = LoginOutcome.InvalidCredentials };
            }

            // The challenge token carries the exact set of factors still pending
            // for this login. A factor not in that set cannot be re-played to
            // bypass another factor's verification.
            bool factorIsPending = payload.RemainingFactors
                .Any(f => string.Equals(f, request.FactorName, StringComparison.OrdinalIgnoreCase));
            if (!factorIsPending)
            {
                return new LoginAttemptResult { Outcome = LoginOutcome.InvalidCredentials, UserId = payload.UserId };
            }

            User? user = await _userManager.FindByIdAsync(payload.UserId.ToString());
            if (user == null)
            {
                return new LoginAttemptResult { Outcome = LoginOutcome.InvalidCredentials };
            }

            bool ok = await _mfa.VerifyAsync(user, request.FactorName, request.Code, ct);
            if (!ok)
            {
                await _log.LogAsync(MfaFailedAction, nameof(User), user.Id.ToString(),
                    $"factor={request.FactorName}", success: false,
                    overrideUserId: user.Id, overrideRoleId: user.RoleId, ct: ct);
                return new LoginAttemptResult { Outcome = LoginOutcome.InvalidCredentials, UserId = user.Id };
            }

            await _log.LogAsync(MfaVerifiedAction, nameof(User), user.Id.ToString(),
                $"factor={request.FactorName}", success: true,
                overrideUserId: user.Id, overrideRoleId: user.RoleId, ct: ct);

            List<string> newRemaining = payload.RemainingFactors
                .Where(f => !string.Equals(f, request.FactorName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (newRemaining.Count == 0)
            {
                LoginResponse tokens = await BuildTokensAsync(user, remoteIp, ct);
                await _log.LogAsync(LoginSuccessAction, nameof(User), user.Id.ToString(),
                    $"email={user.Email};mfa=completed", success: true,
                    overrideUserId: user.Id, overrideRoleId: user.RoleId, ct: ct);
                return new LoginAttemptResult { Outcome = LoginOutcome.Ok, Response = tokens, UserId = user.Id };
            }

            IssuedMfaChallenge nextChallenge = _jwt.IssueMfaChallengeToken(user, newRemaining);
            await _log.LogAsync(LoginMfaChallengeAction, nameof(User), user.Id.ToString(),
                $"step;remaining={string.Join(",", newRemaining)}", success: true,
                overrideUserId: user.Id, overrideRoleId: user.RoleId, ct: ct);

            return new LoginAttemptResult
            {
                Outcome = LoginOutcome.MfaRequired,
                UserId = user.Id,
                Response = new LoginResponse
                {
                    RequiresMfa = true,
                    MfaChallengeToken = nextChallenge.Token,
                    AvailableFactors = newRemaining,
                },
            };
        }

        public async Task<RegisterAttemptResult> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
        {
            User? existing = await _userManager.FindByEmailAsync(request.Email);
            if (existing != null)
            {
                await _log.LogAsync(RegisterAction, nameof(User), null,
                    $"email={request.Email};error=EmailAlreadyExists", success: false, ct: ct);
                return new RegisterAttemptResult { Outcome = RegisterOutcome.EmailAlreadyExists };
            }

            Role? customerRole = await _roleManager.FindByNameAsync(DefaultCustomerRole);
            if (customerRole == null)
            {
                await _log.LogAsync(RegisterAction, nameof(User), null,
                    $"email={request.Email};error=DefaultRoleMissing", success: false, ct: ct);
                return new RegisterAttemptResult { Outcome = RegisterOutcome.DefaultRoleMissing };
            }

            var user = new User
            {
                UserName = request.Email,
                Email = request.Email,
                FirstName = request.FirstName,
                Surname = request.Surname,
                RoleId = customerRole.Id,
            };

            IdentityResult create = await _userManager.CreateAsync(user, request.Password);
            if (!create.Succeeded)
            {
                string err = string.Join("; ", create.Errors.Select(e => e.Code));
                await _log.LogAsync(RegisterAction, nameof(User), null,
                    $"email={request.Email};error={err}", success: false, ct: ct);
                return new RegisterAttemptResult
                {
                    Outcome = RegisterOutcome.InvalidPassword,
                    ErrorMessage = string.Join("; ", create.Errors.Select(e => e.Description)),
                };
            }

            await _userManager.AddToRoleAsync(user, customerRole.Name!);

            string confirmToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            await _email.SendAsync(
                to: user.Email!,
                subject: "Confirm your email",
                body: $"Confirmation token for {user.Email}:\n{confirmToken}",
                ct);

            await _log.LogAsync(RegisterAction, nameof(User), user.Id.ToString(),
                $"email={request.Email}", success: true,
                overrideUserId: user.Id, overrideRoleId: user.RoleId, ct: ct);

            // Don't issue a session here: RequireConfirmedEmail is on, so the
            // user must confirm via the emailed token before they can sign in.
            return new RegisterAttemptResult { Outcome = RegisterOutcome.Ok };
        }

        public async Task<RefreshAttemptResult> RefreshAsync(string rawRefreshToken, string? remoteIp, CancellationToken ct = default)
        {
            RefreshOutcome outcome = await _refresh.RotateAsync(rawRefreshToken, remoteIp, ct);

            if (outcome.Result == RefreshResult.Reuse && outcome.UserId.HasValue)
            {
                await _log.LogAsync(RefreshReuseAction, nameof(User), outcome.UserId.Value.ToString(),
                    "refresh_token_reuse", success: false,
                    overrideUserId: outcome.UserId.Value, ct: ct);
                return new RefreshAttemptResult { Outcome = RefreshOutcomeResult.Reuse };
            }

            if (outcome.Result != RefreshResult.Ok || outcome.NewRawToken == null || !outcome.UserId.HasValue)
            {
                return new RefreshAttemptResult { Outcome = RefreshOutcomeResult.Invalid };
            }

            User? user = await _userManager.FindByIdAsync(outcome.UserId.Value.ToString());
            if (user == null)
            {
                return new RefreshAttemptResult { Outcome = RefreshOutcomeResult.Invalid };
            }

            (string roleName, IReadOnlyCollection<string> permissions) = await GetRoleAndPermissionsAsync(user, ct);
            IssuedAccessToken access = _jwt.IssueAccessToken(user, roleName, permissions);
            AuthUserDto userDto = ToDto(user, roleName, permissions);

            await _log.LogAsync(RefreshAction, nameof(User), user.Id.ToString(),
                null, success: true, overrideUserId: user.Id, overrideRoleId: user.RoleId, ct: ct);

            return new RefreshAttemptResult
            {
                Outcome = RefreshOutcomeResult.Ok,
                Response = new LoginResponse
                {
                    AccessToken = access.Token,
                    AccessTokenExpiresAt = access.ExpiresAt,
                    RefreshToken = outcome.NewRawToken,
                    RefreshTokenExpiresAt = outcome.NewExpiresAt,
                    User = userDto,
                },
            };
        }

        public async Task LogoutAsync(int userId, string? refreshToken, CancellationToken ct = default)
        {
            if (!string.IsNullOrEmpty(refreshToken))
            {
                await _refresh.RevokeAsync(refreshToken, "logout", ct);
            }
            await _log.LogAsync(LogoutAction, nameof(User), userId.ToString(),
                null, success: true, overrideUserId: userId, ct: ct);
        }

        public async Task ForgotPasswordAsync(string email, CancellationToken ct = default)
        {
            User? user = await _userManager.FindByEmailAsync(email);
            if (user != null)
            {
                string token = await _userManager.GeneratePasswordResetTokenAsync(user);
                await _email.SendAsync(
                    to: user.Email!,
                    subject: "Reset your password",
                    body: $"Reset token for {user.Email}:\n{token}",
                    ct);
                await _log.LogAsync(PasswordResetRequestedAction, nameof(User), user.Id.ToString(),
                    $"email={email}", success: true,
                    overrideUserId: user.Id, overrideRoleId: user.RoleId, ct: ct);
            }
            else
            {
                await _log.LogAsync(PasswordResetRequestedAction, nameof(User), null,
                    $"email={email};reason=UserNotFound", success: false, ct: ct);
            }
        }

        public async Task<ResetPasswordResult> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default)
        {
            User? user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                // Mirror forgot-password: don't leak which emails exist.
                await _log.LogAsync(PasswordResetCompletedAction, nameof(User), null,
                    $"email={request.Email};reason=UserNotFound", success: false, ct: ct);
                return ResetPasswordResult.Fail("Reset failed — token may be expired or invalid.");
            }

            IdentityResult result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
            if (!result.Succeeded)
            {
                string codes = string.Join(",", result.Errors.Select(e => e.Code));
                await _log.LogAsync(PasswordResetCompletedAction, nameof(User), user.Id.ToString(),
                    $"errors={codes}", success: false,
                    overrideUserId: user.Id, overrideRoleId: user.RoleId, ct: ct);

                // Surface password-policy errors verbatim so the user can fix
                // them. Token errors stay generic so we don't help an attacker
                // probe valid tokens.
                bool tokenError = result.Errors.Any(e =>
                    e.Code.Contains("Token", StringComparison.OrdinalIgnoreCase));
                string message = tokenError
                    ? "Reset failed — token may be expired or invalid."
                    : string.Join(" ", result.Errors.Select(e => e.Description));
                return ResetPasswordResult.Fail(message);
            }

            await _refresh.RevokeAllForUserAsync(user.Id, "password_reset", ct);
            await _log.LogAsync(PasswordResetCompletedAction, nameof(User), user.Id.ToString(),
                null, success: true, overrideUserId: user.Id, overrideRoleId: user.RoleId, ct: ct);
            return ResetPasswordResult.Success();
        }

        public async Task<bool> ConfirmEmailAsync(string email, string token, CancellationToken ct = default)
        {
            User? user = await _userManager.FindByEmailAsync(email);
            if (user == null) return false;
            IdentityResult result = await _userManager.ConfirmEmailAsync(user, token);
            if (!result.Succeeded) return false;
            await _log.LogAsync(EmailConfirmedAction, nameof(User), user.Id.ToString(),
                null, success: true, overrideUserId: user.Id, overrideRoleId: user.RoleId, ct: ct);
            return true;
        }

        public async Task<MfaEnableSetupResponse?> BeginEnableMfaAsync(int userId, string factorName, CancellationToken ct = default)
        {
            User? user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return null;
            IMfaFactor? factor = _mfa.GetFactor(factorName);
            if (factor == null) return null;

            var resp = new MfaEnableSetupResponse
            {
                FactorName = factor.Name,
                RequiresVerification = true,
            };

            if (string.Equals(factor.Name, TotpAuthenticatorFactor.FactorName, StringComparison.OrdinalIgnoreCase))
            {
                string? key = await _userManager.GetAuthenticatorKeyAsync(user);
                if (string.IsNullOrEmpty(key))
                {
                    await _userManager.ResetAuthenticatorKeyAsync(user);
                    key = await _userManager.GetAuthenticatorKeyAsync(user);
                }
                resp.SharedKey = key;
                resp.AuthenticatorUri = TotpUri.Build(TotpUri.Issuer, user.Email ?? "", key!);
            }
            else if (string.Equals(factor.Name, EmailOtpFactor.FactorName, StringComparison.OrdinalIgnoreCase))
            {
                await factor.SendChallengeAsync(user, ct);
            }

            return resp;
        }

        public async Task<bool> VerifyAndEnableMfaAsync(int userId, MfaVerifySetupRequest request, CancellationToken ct = default)
        {
            User? user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return false;
            IMfaFactor? factor = _mfa.GetFactor(request.FactorName);
            if (factor == null) return false;

            bool ok = await factor.VerifyAsync(user, request.Code, ct);
            if (!ok) return false;

            await using AppDbContext db = await _dbFactory.CreateDbContextAsync(ct);
            bool already = await db.UserMfaFactors
                .AnyAsync(f => f.UserId == user.Id && f.FactorName == factor.Name, ct);
            if (!already)
            {
                db.UserMfaFactors.Add(new UserMfaFactor
                {
                    UserId = user.Id,
                    FactorName = factor.Name,
                });
                await db.SaveChangesAsync(ct);
            }

            // Identity treats TwoFactorEnabled as the global flag — flip it on
            // when any factor is enabled so SignInManager respects it.
            if (!user.TwoFactorEnabled)
            {
                user.TwoFactorEnabled = true;
                await _userManager.UpdateAsync(user);
            }

            await _log.LogAsync(MfaEnabledAction, nameof(User), user.Id.ToString(),
                $"factor={factor.Name}", success: true,
                overrideUserId: user.Id, overrideRoleId: user.RoleId, ct: ct);
            return true;
        }

        public async Task<bool> DisableMfaAsync(int userId, string factorName, CancellationToken ct = default)
        {
            User? user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return false;

            await using AppDbContext db = await _dbFactory.CreateDbContextAsync(ct);
            UserMfaFactor? row = await db.UserMfaFactors
                .FirstOrDefaultAsync(f => f.UserId == user.Id && f.FactorName == factorName, ct);
            if (row != null)
            {
                db.UserMfaFactors.Remove(row);
                await db.SaveChangesAsync(ct);
            }

            bool anyLeft = await db.UserMfaFactors.AnyAsync(f => f.UserId == user.Id, ct);
            if (!anyLeft && user.TwoFactorEnabled)
            {
                user.TwoFactorEnabled = false;
                await _userManager.UpdateAsync(user);
            }

            await _log.LogAsync(MfaDisabledAction, nameof(User), user.Id.ToString(),
                $"factor={factorName}", success: true,
                overrideUserId: user.Id, overrideRoleId: user.RoleId, ct: ct);
            return true;
        }

        public async Task<IReadOnlyList<MfaStatusDto>> GetMfaStatusAsync(int userId, CancellationToken ct = default)
        {
            User? user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return Array.Empty<MfaStatusDto>();
            IReadOnlyList<MfaFactorStatus> statuses = await _mfa.GetAllFactorStatusAsync(user, ct);
            return statuses.Select(s => new MfaStatusDto { Name = s.Name, Enabled = s.Enabled }).ToList();
        }

        public async Task<AuthUserDto?> GetCurrentUserAsync(int userId, CancellationToken ct = default)
        {
            User? user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return null;
            return await BuildUserDtoAsync(user, ct);
        }

        private async Task<LoginResponse> BuildTokensAsync(User user, string? remoteIp, CancellationToken ct)
        {
            (string roleName, IReadOnlyCollection<string> permissions) = await GetRoleAndPermissionsAsync(user, ct);
            IssuedAccessToken access = _jwt.IssueAccessToken(user, roleName, permissions);
            IssuedRefreshToken refresh = await _refresh.IssueAsync(user.Id, remoteIp, ct);

            return new LoginResponse
            {
                AccessToken = access.Token,
                AccessTokenExpiresAt = access.ExpiresAt,
                RefreshToken = refresh.RawToken,
                RefreshTokenExpiresAt = refresh.ExpiresAt,
                User = ToDto(user, roleName, permissions),
            };
        }

        private async Task<AuthUserDto> BuildUserDtoAsync(User user, CancellationToken ct)
        {
            (string roleName, IReadOnlyCollection<string> permissions) = await GetRoleAndPermissionsAsync(user, ct);
            return ToDto(user, roleName, permissions);
        }

        private async Task<(string RoleName, IReadOnlyCollection<string> Permissions)> GetRoleAndPermissionsAsync(User user, CancellationToken ct)
        {
            await using AppDbContext db = await _dbFactory.CreateDbContextAsync(ct);
            Role? role = await db.Roles
                .Include(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(r => r.Id == user.RoleId, ct);

            string roleName = role?.Name ?? "";
            List<string> permissions = role?.RolePermissions
                .Select(rp => rp.Permission.Code)
                .OrderBy(c => c)
                .ToList() ?? new List<string>();
            return (roleName, permissions);
        }

        private static AuthUserDto ToDto(User user, string roleName, IReadOnlyCollection<string> permissions)
        {
            return new AuthUserDto
            {
                Id = user.Id,
                Email = user.Email ?? "",
                FirstName = user.FirstName,
                Surname = user.Surname,
                Role = roleName,
                Permissions = permissions.ToList(),
            };
        }

    }
}

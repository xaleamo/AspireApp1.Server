using System.Security.Claims;
using AspireApp1.Server.DTO;
using AspireApp1.Server.Services;
using AspireApp1.Server.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AspireApp1.Server.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private const string RefreshCookieName = "rt";

        private readonly AuthService _auth;
        private readonly IJwtTokenService _jwt;

        public AuthController(AuthService auth, IJwtTokenService jwt)
        {
            _auth = auth;
            _jwt = jwt;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
        {
            LoginAttemptResult result = await _auth.LoginAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString(), ct);

            return result.Outcome switch
            {
                LoginOutcome.InvalidCredentials => Unauthorized(new { message = "Invalid credentials" }),
                LoginOutcome.LockedOut => StatusCode(StatusCodes.Status423Locked,
                    new { message = "Account locked due to failed attempts. Try again later." }),
                LoginOutcome.Blocked => BlockedResult(result.RetryAfterSeconds ?? 60),
                LoginOutcome.EmailNotConfirmed => StatusCode(StatusCodes.Status403Forbidden, new
                {
                    code = "EmailNotConfirmed",
                    message = "Please confirm your email before signing in. Check your inbox for the confirmation code.",
                }),
                LoginOutcome.MfaRequired => Ok(result.Response),
                LoginOutcome.Ok => ReturnLogin(result.Response!),
                _ => StatusCode(500),
            };
        }

        private IActionResult BlockedResult(int retryAfterSeconds)
        {
            Response.Headers["Retry-After"] = retryAfterSeconds.ToString();
            return StatusCode(StatusCodes.Status429TooManyRequests, new
            {
                message = "This account is temporarily blocked due to too many failed login attempts. Try again later.",
                retryAfterSeconds,
            });
        }

        [HttpPost("login/2fa")]
        public async Task<IActionResult> LoginMfa([FromBody] MfaLoginRequest request, CancellationToken ct)
        {
            LoginAttemptResult result = await _auth.VerifyMfaAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
            return result.Outcome switch
            {
                LoginOutcome.Ok => ReturnLogin(result.Response!),
                LoginOutcome.MfaRequired => Ok(result.Response),
                _ => Unauthorized(new { message = "Invalid code" }),
            };
        }

        [HttpPost("login/2fa/send")]
        public async Task<IActionResult> SendMfaChallenge([FromBody] MfaSendRequest request, CancellationToken ct)
        {
            MfaChallengePayload? payload = _jwt.ValidateMfaChallengeToken(request.MfaChallengeToken);
            if (payload == null) return Unauthorized(new { message = "Invalid or expired MFA challenge" });

            AuthUserDto? me = await _auth.GetCurrentUserAsync(payload.UserId, ct);
            if (me == null) return Unauthorized();

            await _auth.BeginEnableMfaAsync(payload.UserId, request.FactorName, ct);
            return NoContent();
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
        {
            RegisterAttemptResult result = await _auth.RegisterAsync(request, ct);
            return result.Outcome switch
            {
                RegisterOutcome.Ok => Ok(new
                {
                    requiresEmailConfirmation = true,
                    email = request.Email,
                    message = "Registration successful. Check your email for the confirmation code.",
                }),
                RegisterOutcome.EmailAlreadyExists => Conflict(new { message = "Email already in use" }),
                RegisterOutcome.DefaultRoleMissing => StatusCode(500, new { message = "Default role is not seeded" }),
                RegisterOutcome.InvalidPassword => BadRequest(new { message = result.ErrorMessage ?? "Invalid password" }),
                _ => StatusCode(500, new { message = "Registration failed" }),
            };
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest? body, CancellationToken ct)
        {
            string? raw = body?.RefreshToken;
            if (string.IsNullOrEmpty(raw) && Request.Cookies.TryGetValue(RefreshCookieName, out string? cookie))
            {
                raw = cookie;
            }
            if (string.IsNullOrEmpty(raw)) return Unauthorized(new { message = "No refresh token" });

            RefreshAttemptResult result = await _auth.RefreshAsync(raw, HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
            return result.Outcome switch
            {
                RefreshOutcomeResult.Ok => ReturnLogin(result.Response!),
                RefreshOutcomeResult.Reuse => Unauthorized(new { message = "Refresh token reuse detected — please log in again" }),
                _ => Unauthorized(new { message = "Invalid refresh token" }),
            };
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RefreshRequest? body, CancellationToken ct)
        {
            int? userId = TryGetUserId();
            if (userId == null) return Unauthorized();

            string? raw = body?.RefreshToken;
            if (string.IsNullOrEmpty(raw) && Request.Cookies.TryGetValue(RefreshCookieName, out string? cookie))
            {
                raw = cookie;
            }
            await _auth.LogoutAsync(userId.Value, raw, ct);

            Response.Cookies.Delete(RefreshCookieName);
            return NoContent();
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken ct)
        {
            await _auth.ForgotPasswordAsync(request.Email, ct);
            // Never disclose whether the email exists.
            return Ok(new { message = "If an account exists for that email, a reset link has been sent." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken ct)
        {
            ResetPasswordResult result = await _auth.ResetPasswordAsync(request, ct);
            return result.Succeeded
                ? NoContent()
                : BadRequest(new { message = result.ErrorMessage ?? "Reset failed." });
        }

        [HttpPost("confirm-email")]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request, CancellationToken ct)
        {
            bool ok = await _auth.ConfirmEmailAsync(request.Email, request.Token, ct);
            return ok ? NoContent() : BadRequest(new { message = "Confirmation failed." });
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me(CancellationToken ct)
        {
            int? userId = TryGetUserId();
            if (userId == null) return Unauthorized();
            AuthUserDto? me = await _auth.GetCurrentUserAsync(userId.Value, ct);
            return me == null ? Unauthorized() : Ok(me);
        }

        [Authorize]
        [HttpGet("mfa")]
        public async Task<IActionResult> GetMfa(CancellationToken ct)
        {
            int? userId = TryGetUserId();
            if (userId == null) return Unauthorized();
            return Ok(await _auth.GetMfaStatusAsync(userId.Value, ct));
        }

        [Authorize]
        [HttpPost("mfa/enable")]
        public async Task<IActionResult> EnableMfa([FromBody] MfaEnableRequest request, CancellationToken ct)
        {
            int? userId = TryGetUserId();
            if (userId == null) return Unauthorized();
            MfaEnableSetupResponse? resp = await _auth.BeginEnableMfaAsync(userId.Value, request.FactorName, ct);
            return resp == null ? BadRequest(new { message = "Unknown factor" }) : Ok(resp);
        }

        [Authorize]
        [HttpPost("mfa/verify-setup")]
        public async Task<IActionResult> VerifyMfaSetup([FromBody] MfaVerifySetupRequest request, CancellationToken ct)
        {
            int? userId = TryGetUserId();
            if (userId == null) return Unauthorized();
            bool ok = await _auth.VerifyAndEnableMfaAsync(userId.Value, request, ct);
            return ok ? NoContent() : BadRequest(new { message = "Invalid code" });
        }

        [Authorize]
        [HttpPost("mfa/disable")]
        public async Task<IActionResult> DisableMfa([FromBody] MfaDisableRequest request, CancellationToken ct)
        {
            int? userId = TryGetUserId();
            if (userId == null) return Unauthorized();
            await _auth.DisableMfaAsync(userId.Value, request.FactorName, ct);
            return NoContent();
        }

        private IActionResult ReturnLogin(LoginResponse response)
        {
            if (!string.IsNullOrEmpty(response.RefreshToken) && response.RefreshTokenExpiresAt.HasValue)
            {
                // Strict is the strongest practical setting for a same-site
                // SPA+API. Requires both sides to share scheme (HTTPS) and
                // registrable domain — true for the localhost dev setup
                // (HTTPS:5173 SPA → HTTPS:7477 API) and for typical prod
                // deployments where the SPA and API share an eTLD+1.
                Response.Cookies.Append(RefreshCookieName, response.RefreshToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Path = "/api/auth",
                    Expires = response.RefreshTokenExpiresAt.Value,
                });
            }
            // Keep the body refresh token for SPA fallback in dev.
            return Ok(response);
        }

        private int? TryGetUserId()
        {
            string? sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
                          ?? User.FindFirstValue("sub");
            return int.TryParse(sub, out int id) ? id : null;
        }
    }
}

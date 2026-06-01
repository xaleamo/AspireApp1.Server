using System.Security.Claims;

namespace AspireApp1.Server.Security
{
    // Runs after UseAuthentication so the principal is hydrated. For any
    // authenticated request, look up the user's email against the blocklist and
    // short-circuit with 429 + Retry-After if a fresh LoginBlocked row exists.
    // Unauthenticated requests bypass — /api/auth/login does its own block
    // check inside AuthService.LoginAsync (it has the email from the body).
    public class LoginBlockMiddleware
    {
        private readonly RequestDelegate _next;

        public LoginBlockMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IUserBlocklist blocklist)
        {
            ClaimsPrincipal? principal = context.User;
            if (principal?.Identity?.IsAuthenticated == true)
            {
                string? email = principal.FindFirstValue(ClaimTypes.Email)
                                ?? principal.FindFirstValue("email");
                if (!string.IsNullOrEmpty(email))
                {
                    BlockStatus status = await blocklist.CheckAsync(email, FailedLoginBurstRule.BlockReasonCode, context.RequestAborted);
                    if (status.IsBlocked)
                    {
                        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                        context.Response.Headers["Retry-After"] = status.RetryAfterSeconds.ToString();
                        await context.Response.WriteAsJsonAsync(new
                        {
                            message = "Your account is temporarily blocked due to too many failed login attempts.",
                            retryAfterSeconds = status.RetryAfterSeconds,
                        });
                        return;
                    }
                }
            }

            await _next(context);
        }
    }

    public static class LoginBlockMiddlewareExtensions
    {
        public static IApplicationBuilder UseLoginBlock(this IApplicationBuilder app)
            => app.UseMiddleware<LoginBlockMiddleware>();
    }
}

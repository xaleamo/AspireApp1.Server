using System.Security.Claims;
using AspireApp1.Server.Services.Auth;

namespace AspireApp1.Server.Models;

// Pulls the JWT claims (user id, role, permission codes) off the current
// HttpContext so GraphQL resolvers can do inline ownership / role checks
// without re-implementing claim parsing each time. Static so it can be reused
// from both Query and Mutation resolvers without a DI dance.
public static class GraphQLAuthContext
{
    public record Caller(int? UserId, string? Role, IReadOnlySet<string> Permissions)
    {
        public bool IsAdmin => Role == "Admin";
        public bool Has(string permissionCode) => Permissions.Contains(permissionCode);
    }

    public static Caller From(IHttpContextAccessor accessor)
    {
        ClaimsPrincipal? principal = accessor.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return new Caller(null, null, new HashSet<string>());
        }

        string? sub = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? principal.FindFirstValue("sub");
        int? userId = int.TryParse(sub, out int id) ? id : null;

        string? role = principal.FindFirstValue(ClaimTypes.Role);

        var perms = principal
            .FindAll(JwtTokenService.PermissionsClaim)
            .Select(c => c.Value)
            .ToHashSet();

        return new Caller(userId, role, perms);
    }
}

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AspireApp1.Server.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AspireApp1.Server.Services.Auth
{
    public class JwtTokenService : IJwtTokenService
    {
        public const string PurposeClaim = "purpose";
        public const string AccessPurpose = "access";
        public const string MfaChallengePurpose = "mfa_challenge";
        public const string PermissionsClaim = "permissions";
        public const string RemainingFactorClaim = "remaining_factor";

        private readonly JwtOptions _options;
        private readonly SigningCredentials _credentials;
        private readonly TokenValidationParameters _challengeValidation;

        public JwtTokenService(IOptions<JwtOptions> options)
        {
            _options = options.Value;
            byte[] keyBytes = Encoding.UTF8.GetBytes(_options.SigningKey);
            var key = new SymmetricSecurityKey(keyBytes);
            _credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            _challengeValidation = new TokenValidationParameters
            {
                ValidIssuer = _options.Issuer,
                ValidAudience = _options.Audience,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
            };
        }

        public IssuedAccessToken IssueAccessToken(User user, string roleName, IReadOnlyCollection<string> permissions)
        {
            string jti = Guid.NewGuid().ToString("N");
            DateTime now = DateTime.UtcNow;
            DateTime exp = now.AddMinutes(_options.AccessTokenMinutes);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(JwtRegisteredClaimNames.Email, user.Email ?? ""),
                new(JwtRegisteredClaimNames.Jti, jti),
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.Email ?? ""),
                new(ClaimTypes.Role, roleName),
                new(PurposeClaim, AccessPurpose),
            };

            foreach (string p in permissions)
            {
                claims.Add(new Claim(PermissionsClaim, p));
            }

            if (!string.IsNullOrEmpty(user.SecurityStamp))
            {
                claims.Add(new Claim("sst", user.SecurityStamp));
            }

            var token = new JwtSecurityToken(
                issuer: _options.Issuer,
                audience: _options.Audience,
                claims: claims,
                notBefore: now,
                expires: exp,
                signingCredentials: _credentials);

            string raw = new JwtSecurityTokenHandler().WriteToken(token);
            return new IssuedAccessToken(raw, exp, jti);
        }

        public IssuedMfaChallenge IssueMfaChallengeToken(User user, IEnumerable<string> remainingFactors)
        {
            DateTime now = DateTime.UtcNow;
            DateTime exp = now.AddMinutes(_options.MfaChallengeMinutes);

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
                new(PurposeClaim, MfaChallengePurpose),
            };
            foreach (string factor in remainingFactors)
            {
                claims.Add(new Claim(RemainingFactorClaim, factor));
            }

            var token = new JwtSecurityToken(
                issuer: _options.Issuer,
                audience: _options.Audience,
                claims: claims,
                notBefore: now,
                expires: exp,
                signingCredentials: _credentials);

            string raw = new JwtSecurityTokenHandler().WriteToken(token);
            return new IssuedMfaChallenge(raw, exp);
        }

        public MfaChallengePayload? ValidateMfaChallengeToken(string token)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                System.Security.Claims.ClaimsPrincipal principal = handler.ValidateToken(token, _challengeValidation, out _);
                string? purpose = principal.FindFirstValue(PurposeClaim);
                if (purpose != MfaChallengePurpose) return null;

                string? sub = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
                              ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(sub, out int id)) return null;

                List<string> remaining = principal.FindAll(RemainingFactorClaim)
                    .Select(c => c.Value)
                    .ToList();
                return new MfaChallengePayload(id, remaining);
            }
            catch
            {
                return null;
            }
        }
    }
}

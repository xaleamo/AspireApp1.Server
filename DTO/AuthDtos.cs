using System.ComponentModel.DataAnnotations;

namespace AspireApp1.Server.DTO
{
    public class LoginRequest
    {
        [Required] public string Email { get; set; } = "";
        [Required] public string Password { get; set; } = "";
    }

    public class RegisterRequest
    {
        [Required] public string Email { get; set; } = "";
        [Required] public string Password { get; set; } = "";
        [Required] public string FirstName { get; set; } = "";
        [Required] public string Surname { get; set; } = "";
    }

    public class AuthUserDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string Surname { get; set; } = "";
        public string Role { get; set; } = "";
        public List<string> Permissions { get; set; } = new();
    }

    public class LoginResponse
    {
        public bool RequiresMfa { get; set; }
        public string? MfaChallengeToken { get; set; }
        public List<string>? AvailableFactors { get; set; }

        public string? AccessToken { get; set; }
        public DateTime? AccessTokenExpiresAt { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiresAt { get; set; }

        public AuthUserDto? User { get; set; }
    }

    public class MfaLoginRequest
    {
        [Required] public string MfaChallengeToken { get; set; } = "";
        [Required] public string FactorName { get; set; } = "";
        [Required] public string Code { get; set; } = "";
    }

    public class MfaSendRequest
    {
        [Required] public string MfaChallengeToken { get; set; } = "";
        [Required] public string FactorName { get; set; } = "";
    }

    public class RefreshRequest
    {
        public string? RefreshToken { get; set; }
    }

    public class ForgotPasswordRequest
    {
        [Required] public string Email { get; set; } = "";
    }

    public class ResetPasswordRequest
    {
        [Required] public string Email { get; set; } = "";
        [Required] public string Token { get; set; } = "";
        [Required] public string NewPassword { get; set; } = "";
    }

    public class ConfirmEmailRequest
    {
        [Required] public string Email { get; set; } = "";
        [Required] public string Token { get; set; } = "";
    }

    public class MfaEnableRequest
    {
        [Required] public string FactorName { get; set; } = "";
    }

    public class MfaEnableSetupResponse
    {
        public string FactorName { get; set; } = "";
        public string? SharedKey { get; set; }
        public string? AuthenticatorUri { get; set; }
        public bool RequiresVerification { get; set; }
    }

    public class MfaVerifySetupRequest
    {
        [Required] public string FactorName { get; set; } = "";
        [Required] public string Code { get; set; } = "";
    }

    public class MfaDisableRequest
    {
        [Required] public string FactorName { get; set; } = "";
    }

    public class MfaStatusDto
    {
        public string Name { get; set; } = "";
        public bool Enabled { get; set; }
    }
}

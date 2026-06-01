namespace AspireApp1.Server.Services.Auth
{
    public class JwtOptions
    {
        public const string SectionName = "Jwt";

        public string Issuer { get; set; } = "";
        public string Audience { get; set; } = "";
        public string SigningKey { get; set; } = "";
        public int AccessTokenMinutes { get; set; } = 15;
        public int RefreshTokenDays { get; set; } = 7;
        public int MfaChallengeMinutes { get; set; } = 5;
    }
}

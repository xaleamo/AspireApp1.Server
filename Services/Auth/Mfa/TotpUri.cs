namespace AspireApp1.Server.Services.Auth.Mfa
{
    public static class TotpUri
    {
        public const string Issuer = "RazlogDesserts";

        public static string Build(string issuer, string accountName, string sharedKey)
        {
            string encIssuer = Uri.EscapeDataString(issuer);
            string encAccount = Uri.EscapeDataString(accountName);
            return $"otpauth://totp/{encIssuer}:{encAccount}?secret={sharedKey}&issuer={encIssuer}&digits=6";
        }
    }
}

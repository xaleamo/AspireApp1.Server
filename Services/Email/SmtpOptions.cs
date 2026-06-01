namespace AspireApp1.Server.Services.Email
{
    public class SmtpOptions
    {
        public const string SectionName = "Smtp";

        public string Host { get; set; } = "smtp.gmail.com";
        public int Port { get; set; } = 587;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FromAddress { get; set; } = string.Empty;
        public string FromDisplayName { get; set; } = "Razlog Desserts";
        public bool UseStartTls { get; set; } = true;
    }
}

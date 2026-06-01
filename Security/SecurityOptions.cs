namespace AspireApp1.Server.Security
{
    public class SecurityOptions
    {
        public const string SectionName = "Security";

        public int ScanIntervalSeconds { get; set; } = 15;

        public int FailedLoginThreshold { get; set; } = 5;
        public int FailedLoginWindowMinutes { get; set; } = 10;

        // After this many failed logins inside the FailedLoginWindow, the
        // account/email is hard-blocked from any request for LoginBlockMinutes.
        public int LoginBlockThreshold { get; set; } = 10;
        public int LoginBlockMinutes { get; set; } = 5;

        public int OrderBurstThreshold { get; set; } = 10;
        public int OrderBlockThreshold { get; set; } = 20;
        public int OrderBurstWindowMinutes { get; set; } = 5;

        // Chat traffic is bursty by nature; a separate window + two-tier
        // threshold lets us observe slightly noisy users without blocking
        // them and only hard-block obvious spam/automation.
        public int ChatBurstThreshold { get; set; } = 15;
        public int ChatBlockThreshold { get; set; } = 30;
        public int ChatBurstWindowMinutes { get; set; } = 1;
    }
}

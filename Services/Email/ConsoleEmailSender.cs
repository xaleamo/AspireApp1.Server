namespace AspireApp1.Server.Services.Email
{
    public class ConsoleEmailSender : IEmailSender
    {
        private readonly ILogger<ConsoleEmailSender> _logger;

        public ConsoleEmailSender(ILogger<ConsoleEmailSender> logger)
        {
            _logger = logger;
        }

        public Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
        {
            _logger.LogInformation(
                "\n========== EMAIL (dev) ==========\nTo: {To}\nSubject: {Subject}\n\n{Body}\n=================================\n",
                to, subject, body);
            return Task.CompletedTask;
        }
    }
}

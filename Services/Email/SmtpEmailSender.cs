using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace AspireApp1.Server.Services.Email
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly SmtpOptions _opts;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(IOptions<SmtpOptions> opts, ILogger<SmtpEmailSender> logger)
        {
            _opts = opts.Value;
            _logger = logger;
        }

        public async Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_opts.FromDisplayName, _opts.FromAddress));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;
            message.Body = new TextPart("plain") { Text = body };

            using var client = new SmtpClient();
            var socketOption = _opts.UseStartTls
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.Auto;

            await client.ConnectAsync(_opts.Host, _opts.Port, socketOption, ct);
            await client.AuthenticateAsync(_opts.Username, _opts.Password, ct);
            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);

            _logger.LogInformation("SMTP email sent to {To} (subject: {Subject})", to, subject);
        }
    }
}

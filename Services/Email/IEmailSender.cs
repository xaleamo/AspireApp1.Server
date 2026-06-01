namespace AspireApp1.Server.Services.Email
{
    public interface IEmailSender
    {
        Task SendAsync(string to, string subject, string body, CancellationToken ct = default);
    }
}

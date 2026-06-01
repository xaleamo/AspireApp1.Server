namespace AspireApp1.Server.Security
{
    public interface IThreatRule
    {
        string Name { get; }

        Task EvaluateAsync(AppDbContext db, DateTime now, CancellationToken ct);
    }
}

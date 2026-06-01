namespace AspireApp1.Server.Security
{
    public record BlockStatus(bool IsBlocked, DateTime? UnblockAt)
    {
        public int RetryAfterSeconds =>
            UnblockAt is null
                ? 0
                : Math.Max(1, (int)Math.Ceiling((UnblockAt.Value - DateTime.UtcNow).TotalSeconds));

        public static readonly BlockStatus NotBlocked = new(false, null);
    }

    public interface IUserBlocklist
    {
        Task<BlockStatus> CheckAsync(string identifier, string reasonCode, CancellationToken ct = default);
    }
}

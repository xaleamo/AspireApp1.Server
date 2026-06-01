namespace AspireApp1.Server.Auditing
{
    public interface ICurrentUserContext
    {
        Task<(int? UserId, int? RoleId)> GetAsync(CancellationToken ct = default);
    }
}

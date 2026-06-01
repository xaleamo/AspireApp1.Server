using AspireApp1.Server.Models;
using AspireApp1.Server.Repositories;

namespace AspireApp1.Server.Auditing
{
    public class LoggingRoleRepository : IRoleRepository
    {
        private const string Repo = nameof(RoleRepository);
        private const string Entity = nameof(Role);

        private readonly IRoleRepository _inner;
        private readonly IActionLogger _logger;

        public LoggingRoleRepository(IRoleRepository inner, IActionLogger logger)
        {
            _inner = inner;
            _logger = logger;
        }

        public Role? GetByName(string name)
        {
            string action = $"{Repo}.{nameof(GetByName)}";
            try
            {
                Role? role = _inner.GetByName(name);
                _ = _logger.LogAsync(action, Entity, role?.Id.ToString(),
                    details: $"name={name}", success: true);
                return role;
            }
            catch (Exception ex)
            {
                _ = _logger.LogAsync(action, Entity, null,
                    details: $"name={name}; error={ex.GetType().Name}", success: false);
                throw;
            }
        }
    }
}

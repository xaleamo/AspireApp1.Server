using AspireApp1.Server.Models;
using AspireApp1.Server.Repositories;

namespace AspireApp1.Server.Auditing
{
    public class LoggingUserRepository : IUserRepository
    {
        private const string Repo = nameof(UserRepository);
        private const string Entity = nameof(User);

        private readonly IUserRepository _inner;
        private readonly IActionLogger _logger;

        public LoggingUserRepository(IUserRepository inner, IActionLogger logger)
        {
            _inner = inner;
            _logger = logger;
        }

        public User? GetByEmail(string email) =>
            Audit($"{Repo}.{nameof(GetByEmail)}",
                details: $"email={email}", entityId: null,
                () => _inner.GetByEmail(email));

        public User? GetById(int id) =>
            Audit($"{Repo}.{nameof(GetById)}", null, id.ToString(),
                () => _inner.GetById(id));

        public List<User> GetAll() =>
            Audit($"{Repo}.{nameof(GetAll)}", null, null, () => _inner.GetAll());

        public bool EmailExists(string email) =>
            Audit($"{Repo}.{nameof(EmailExists)}",
                details: $"email={email}", entityId: null,
                () => _inner.EmailExists(email));

        public User Add(User user)
        {
            string action = $"{Repo}.{nameof(Add)}";
            string details = $"email={user.Email}";
            try
            {
                User created = _inner.Add(user);
                _ = _logger.LogAsync(action, Entity, created.Id.ToString(), details,
                    success: true);
                return created;
            }
            catch (Exception ex)
            {
                _ = _logger.LogAsync(action, Entity, null,
                    $"{details}; error={ex.GetType().Name}", success: false);
                throw;
            }
        }

        private T Audit<T>(string action, string? details, string? entityId, Func<T> op)
        {
            try
            {
                T result = op();
                _ = _logger.LogAsync(action, Entity, entityId, details, success: true);
                return result;
            }
            catch (Exception ex)
            {
                _ = _logger.LogAsync(action, Entity, entityId,
                    details: $"{details}; error={ex.GetType().Name}",
                    success: false);
                throw;
            }
        }
    }
}

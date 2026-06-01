using AspireApp1.Server.Models;
using AspireApp1.Server.Repositories;

namespace AspireApp1.Server.Auditing
{
    public class LoggingChatRepository : IChatRepository
    {
        private const string Repo = nameof(ChatRepository);
        private const string Entity = nameof(ChatMessage);

        private readonly IChatRepository _inner;
        private readonly IActionLogger _logger;

        public LoggingChatRepository(IChatRepository inner, IActionLogger logger)
        {
            _inner = inner;
            _logger = logger;
        }

        public async Task<List<ChatMessage>> GetRecentAsync(int limit = 50)
        {
            string action = $"{Repo}.{nameof(GetRecentAsync)}";
            try
            {
                List<ChatMessage> result = await _inner.GetRecentAsync(limit);
                _ = _logger.LogAsync(action, Entity, null, $"limit={limit}", success: true);
                return result;
            }
            catch (Exception ex)
            {
                _ = _logger.LogAsync(action, Entity, null,
                    $"limit={limit}; error={ex.GetType().Name}", success: false);
                throw;
            }
        }

        public async Task AddAsync(ChatMessage message)
        {
            string action = $"{Repo}.{nameof(AddAsync)}";
            int? senderId = message.Sender?.Id;
            string details = $"senderId={senderId};len={message.Text?.Length ?? 0}";
            try
            {
                await _inner.AddAsync(message);
                _ = _logger.LogAsync(action, Entity, message.Id, details,
                    success: true,
                    overrideUserId: senderId);
            }
            catch (Exception ex)
            {
                _ = _logger.LogAsync(action, Entity, null,
                    $"{details}; error={ex.GetType().Name}", success: false);
                throw;
            }
        }
    }
}

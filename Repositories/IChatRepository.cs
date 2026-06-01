using AspireApp1.Server.Models;

namespace AspireApp1.Server.Repositories
{
    public interface IChatRepository
    {
        Task<List<ChatMessage>> GetRecentAsync(int limit = 50);
        Task AddAsync(ChatMessage message);
    }
}

using AspireApp1.Server.Configuration;
using AspireApp1.Server.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace AspireApp1.Server.Repositories
{
    public class ChatRepository : IChatRepository
    {
        private readonly IMongoCollection<ChatMessage> _messages;

        public ChatRepository(IOptions<MongoOptions> options)
        {
            MongoOptions opts = options.Value;
            MongoClient client = new MongoClient(opts.ConnectionString);
            IMongoDatabase database = client.GetDatabase(opts.Database);
            _messages = database.GetCollection<ChatMessage>(opts.MessagesCollection);
        }

        public async Task<List<ChatMessage>> GetRecentAsync(int limit = 50)
        {
            List<ChatMessage> recent = await _messages
                .Find(FilterDefinition<ChatMessage>.Empty)
                .SortByDescending(m => m.SentAt)
                .Limit(limit)
                .ToListAsync();

            recent.Reverse();
            return recent;
        }

        public async Task AddAsync(ChatMessage message)
        {
            await _messages.InsertOneAsync(message);
        }
    }
}

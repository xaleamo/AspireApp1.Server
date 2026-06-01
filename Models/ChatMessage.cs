using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AspireApp1.Server.Models
{
    [BsonIgnoreExtraElements]
    public class ChatMessage
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = "";

        public ChatSender Sender { get; set; } = new();
        public string Text { get; set; } = "";
        public DateTime SentAt { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class ChatSender
    {
        public int Id { get; set; }
        public string Email { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string Surname { get; set; } = "";
        public string Role { get; set; } = "";
    }
}

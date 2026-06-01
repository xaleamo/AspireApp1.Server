using AspireApp1.Server.DTO;
using AspireApp1.Server.Models;
using AspireApp1.Server.Repositories;

namespace AspireApp1.Server.Services
{
    public class ChatService
    {
        private readonly IChatRepository _repo;

        public ChatService(IChatRepository repo)
        {
            _repo = repo;
        }

        public async Task<List<ChatMessageDto>> GetRecentAsync(int limit = 50)
        {
            List<ChatMessage> messages = await _repo.GetRecentAsync(limit);
            return messages.Select(ToDto).ToList();
        }

        public async Task<ChatMessageDto> SendAsync(SendMessageDto dto)
        {
            ChatMessage message = new ChatMessage
            {
                Sender = new ChatSender
                {
                    Id = dto.Sender.Id,
                    Email = dto.Sender.Email,
                    FirstName = dto.Sender.FirstName,
                    Surname = dto.Sender.Surname,
                    Role = dto.Sender.Role,
                },
                Text = dto.Text,
                SentAt = DateTime.UtcNow,
            };
            await _repo.AddAsync(message);
            return ToDto(message);
        }

        public static ChatMessageDto ToDto(ChatMessage m) => new ChatMessageDto
        {
            Id = m.Id,
            Sender = new ChatSenderDto
            {
                Id = m.Sender?.Id ?? 0,
                Email = m.Sender?.Email ?? "",
                FirstName = m.Sender?.FirstName ?? "",
                Surname = m.Sender?.Surname ?? "",
                Role = m.Sender?.Role ?? "",
            },
            Text = m.Text,
            SentAt = m.SentAt,
        };
    }
}

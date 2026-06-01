using System.Security.Claims;
using AspireApp1.Server.DTO;
using AspireApp1.Server.Security;
using AspireApp1.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace AspireApp1.Server.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ChatService _service;
        private readonly IUserBlocklist _blocklist;

        public ChatHub(ChatService service, IUserBlocklist blocklist)
        {
            _service = service;
            _blocklist = blocklist;
        }

        public async Task SendMessage(SendMessageDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Text))
            {
                return;
            }

            string? sub = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(sub, out int userId))
            {
                throw new HubException("Not authenticated.");
            }

            BlockStatus status = await _blocklist.CheckAsync(
                $"user:{userId}", ChatBurstRule.BlockReasonCode, Context.ConnectionAborted);
            if (status.IsBlocked)
            {
                // Send a typed event to the caller and return rather than
                // throwing. Two scalar args (string + int) avoid any
                // JSON property-naming gotchas during deserialization.
                string blockedMessage =
                    $"You are temporarily blocked from sending messages. Try again in {status.RetryAfterSeconds}s.";
                await Clients.Caller.SendAsync(
                    "Blocked", blockedMessage, status.RetryAfterSeconds, Context.ConnectionAborted);
                return;
            }

            // Override the client-supplied sender id with the trusted JWT-derived
            // id so the audit log entry written inside LoggingChatRepository
            // (via overrideUserId = message.Sender.Id) reflects the real sender.
            dto.Sender.Id = userId;

            ChatMessageDto saved = await _service.SendAsync(dto);
            await Clients.All.SendAsync("ReceiveMessage", saved);
        }
    }
}

using AspireApp1.Server.DTO;
using AspireApp1.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AspireApp1.Server.Controllers
{
    [ApiController]
    [Route("api/chat")]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly ChatService _service;

        public ChatController(ChatService service)
        {
            _service = service;
        }

        [HttpGet("history")]
        public async Task<ActionResult<List<ChatMessageDto>>> GetHistory([FromQuery] int limit = 50)
        {
            int safeLimit = Math.Clamp(limit, 1, 200);
            List<ChatMessageDto> messages = await _service.GetRecentAsync(safeLimit);
            return Ok(messages);
        }
    }
}

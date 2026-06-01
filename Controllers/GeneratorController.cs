using AspireApp1.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AspireApp1.Server.Controllers
{
    [ApiController]
    //[AllowAnonymous]
    [Route("api/generator")]
    [Authorize(Roles = "Admin")]
    public class GeneratorController : ControllerBase
    {
        private readonly GeneratorService _generator;

        public GeneratorController(GeneratorService generator)
            => _generator = generator;

        [HttpPost("start")]
        public IActionResult Start([FromQuery] int batchSize = 5, [FromQuery] int intervalMs = 3000)
        {
            _generator.Start(batchSize, intervalMs);
            return Ok(new { status = "started" });
        }

        [HttpPost("stop")]
        public IActionResult Stop()
        {
            _generator.Stop();
            return Ok(new { status = "stopped" });
        }

        [HttpGet("status")]
        public IActionResult Status()
            => Ok(new { running = _generator.IsRunning });
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AspireApp1.Server.Services;

namespace AspireApp1.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    //[AllowAnonymous]
    public class StatisticsController : ControllerBase
    {
        private readonly StatisticsService _service;

        public StatisticsController(StatisticsService service)
        {
            _service = service;
        }

        [HttpGet]
        [Authorize(Policy = "perm:statistics:view")]
        public IActionResult Get()
        {
            return Ok(_service.GetStatistics());
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using AspireApp1.Server.Services;
using AspireApp1.Server.DTO;
using AspireApp1.Server.Models;

namespace AspireApp1.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DessertsController : ControllerBase
    {
        private readonly DessertService _service;

        public DessertsController(DessertService service)
        {
            _service = service;
        }

        [HttpGet]
        [Authorize(Policy = "perm:desserts:read")]
        public IActionResult GetAll([FromQuery] int page = 1,
                                    [FromQuery] int pageSize = 6,
                                    [FromQuery] string? searchName=null)
        {
            PagedResultDto<DessertSummaryDto> result = _service.GetPaged(page, pageSize,searchName);
            return Ok(result);
        }

        [HttpGet("{id}")]
        [Authorize(Policy = "perm:desserts:read")]
        public IActionResult GetById(int id)
        {
            DessertDetailDto? dto = _service.GetById(id);
            if (dto == null) return NotFound();
            return Ok(dto);
        }

        [HttpPost]
        [Authorize(Policy = "perm:desserts:create")]
        public IActionResult Create([FromBody] DessertDetailDto dto)
        {
            DessertDetailDto created = _service.Add(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "perm:desserts:update")]
        public IActionResult Update(int id, [FromBody] DessertDetailDto dto)
        {
            DessertDetailDto? updated = _service.Update(id, dto);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "perm:desserts:delete")]
        public IActionResult Delete(int id)
        {
            bool deleted = _service.Delete(id);
            if (!deleted) return NotFound();
            return NoContent();
        }

        [HttpPost("sync")]
        [Authorize(Roles = "Admin")]
        public IActionResult Sync([FromBody] List<SyncOperation> operations)
        {
            foreach (var op in operations)
            {
                switch (op.Type)
                {
                    case "CREATE" when op.Payload != null:
                        _service.Add(op.Payload);
                        break;

                    case "UPDATE" when op.Payload != null && op.TargetId != null:
                        _service.Update(op.TargetId.Value, op.Payload);
                        break;

                    case "DELETE" when op.TargetId != null:
                        _service.Delete(op.TargetId.Value);
                        break;
                }
            }

            return Ok();
        }
    }
}

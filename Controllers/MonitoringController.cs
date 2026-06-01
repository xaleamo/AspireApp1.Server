using System.Security.Claims;
using AspireApp1.Server.Auditing;
using AspireApp1.Server.DTO;
using AspireApp1.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AspireApp1.Server.Controllers
{
    [ApiController]
    [Route("api/monitoring")]
    [Authorize(Roles = "Admin")]
    public class MonitoringController : ControllerBase
    {
        private readonly AppDbContext _db;

        public MonitoringController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("logs")]
        public async Task<ActionResult<PagedActionLogDto>> GetLogs(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] int? userId = null,
            [FromQuery] string? actionType = null,
            [FromQuery] bool? success = null,
            [FromQuery] bool excludeReads = false,
            [FromQuery(Name = "entityTypes")] string[]? entityTypes = null)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 200);

            IQueryable<ActionLog> q = _db.ActionLogs.AsNoTracking();

            if (userId.HasValue) q = q.Where(l => l.UserId == userId);
            if (!string.IsNullOrWhiteSpace(actionType))
                q = q.Where(l => l.ActionType == actionType);
            if (success.HasValue) q = q.Where(l => l.Success == success);
            if (excludeReads)
                q = q.Where(l => !EF.Functions.ILike(l.ActionType, "%get%"));
            if (entityTypes is { Length: > 0 })
            {
                List<string> wanted = entityTypes
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .ToList();
                if (wanted.Count > 0)
                {
                    q = q.Where(l => l.EntityType != null && wanted.Contains(l.EntityType));
                }
            }

            int total = await q.CountAsync();

            List<ActionLogDto> items = await q
                .OrderByDescending(l => l.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(l => new ActionLogDto
                {
                    Id = l.Id,
                    UserId = l.UserId,
                    UserEmail = l.User != null ? l.User.Email : null,
                    RoleId = l.RoleId,
                    RoleName = l.Role != null ? l.Role.Name : null,
                    ActionType = l.ActionType,
                    EntityType = l.EntityType,
                    EntityId = l.EntityId,
                    Details = l.Details,
                    Success = l.Success,
                    Timestamp = l.Timestamp,
                })
                .ToListAsync();

            return Ok(new PagedActionLogDto
            {
                Page = page,
                PageSize = pageSize,
                Total = total,
                Items = items,
            });
        }

        [HttpGet("observation-list")]
        public async Task<ActionResult<List<MonitoredUserDto>>> GetObservationList(
            [FromQuery] bool includeResolved = false)
        {
            IQueryable<MonitoredUser> q = _db.MonitoredUsers.AsNoTracking();
            if (!includeResolved) q = q.Where(m => !m.Resolved);

            List<MonitoredUserDto> items = await q
                .OrderByDescending(m => m.FlaggedAt)
                .Select(m => new MonitoredUserDto
                {
                    Id = m.Id,
                    UserId = m.UserId,
                    UserEmail = m.User != null ? m.User.Email : null,
                    Identifier = m.Identifier,
                    Reason = m.Reason,
                    FlaggedAt = m.FlaggedAt,
                    WindowStart = m.WindowStart,
                    HitCount = m.HitCount,
                    Resolved = m.Resolved,
                    ResolvedAt = m.ResolvedAt,
                    ResolvedByUserId = m.ResolvedByUserId,
                })
                .ToListAsync();

            return Ok(items);
        }

        [HttpPost("observation-list/{id:int}/resolve")]
        public async Task<IActionResult> ResolveObservation(int id)
        {
            MonitoredUser? row = await _db.MonitoredUsers.FirstOrDefaultAsync(m => m.Id == id);
            if (row == null) return NotFound();
            if (row.Resolved) return NoContent();

            int? resolvedBy = null;
            string? sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(sub, out int parsed))
            {
                resolvedBy = parsed;
            }

            row.Resolved = true;
            row.ResolvedAt = DateTime.UtcNow;
            row.ResolvedByUserId = resolvedBy;
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}

using EvolutionApi.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EvolutionApi.API.Controllers.Platform;

[ApiController]
[Route("api/v1/audit-logs")]
public sealed class AuditLogsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? tenantId = null,
        CancellationToken ct = default)
    {
        var query = db.AuditLogs.AsNoTracking();
        if (tenantId != null)
            query = query.Where(x => x.TenantId == tenantId);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.OccurredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.TenantId,
                x.UserEmail,
                x.Action,
                x.EntityType,
                x.EntityId,
                x.IpAddress,
                x.OccurredAt
            })
            .ToListAsync(ct);

        return Ok(new { items, totalCount = total, page, pageSize });
    }
}


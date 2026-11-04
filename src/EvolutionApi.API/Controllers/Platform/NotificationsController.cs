using EvolutionApi.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EvolutionApi.API.Controllers.Platform;

[ApiController]
[Route("api/v1/notifications")]
public sealed class NotificationsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetNotifications(CancellationToken ct)
    {
        var items = await db.Notifications
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Take(20)
            .Select(x => new
            {
                x.Id,
                x.TenantId,
                Type = x.Type.ToString(),
                x.Title,
                x.Body,
                x.ActionUrl,
                x.IsRead,
                x.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpPost("{id}/read")]
    public async Task<IActionResult> MarkRead(string id, CancellationToken ct)
    {
        var n = await db.Notifications.FindAsync([id], ct);
        if (n is null) return NotFound();

        n.IsRead = true;
        n.ReadAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        return NoContent();
    }
}


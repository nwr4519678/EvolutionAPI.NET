using EvolutionApi.Domain.Enums;
using EvolutionApi.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EvolutionApi.API.Controllers.Platform;

[ApiController]
[Route("api/v1/platform")]
public sealed class PlatformOverviewController(AppDbContext db) : ControllerBase
{
    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview(CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var startOfToday = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, TimeSpan.Zero);
        var startOfWeek = startOfToday.AddDays(-(int)now.DayOfWeek);
        var startOfMonth = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);

        var totalClients = await db.Tenants.CountAsync(ct);
        var activeClients = await db.Tenants.CountAsync(x => x.Status == TenantStatus.Active, ct);

        var totalNumbers = await db.WhatsAppNumbers.CountAsync(ct);
        var connectedNumbers = await db.WhatsAppNumbers.CountAsync(x => x.Status == WhatsAppNumberStatus.Connected, ct);
        var disconnectedNumbers = totalNumbers - connectedNumbers;

        var totalConversations = await db.Conversations.CountAsync(ct);
        var openConversations = await db.Conversations.CountAsync(x => x.Status == ConversationStatus.Open, ct);

        var messagesToday = await db.UsageRecords.CountAsync(x => x.OccurredAt >= startOfToday, ct);
        var messagesThisWeek = await db.UsageRecords.CountAsync(x => x.OccurredAt >= startOfWeek, ct);
        var messagesThisMonth = await db.UsageRecords.CountAsync(x => x.OccurredAt >= startOfMonth, ct);

        var tokensToday = await db.UsageRecords
            .Where(x => x.OccurredAt >= startOfToday)
            .SumAsync(x => (long)(x.TokensIn + x.TokensOut), ct);

        var costToday = await db.UsageRecords
            .Where(x => x.OccurredAt >= startOfToday)
            .SumAsync(x => x.Cost, ct);

        var costThisMonth = await db.UsageRecords
            .Where(x => x.OccurredAt >= startOfMonth)
            .SumAsync(x => x.Cost, ct);

        var aiCallsCount = await db.UsageRecords
            .CountAsync(x => x.EventType == UsageEventType.AiCall || x.EventType == UsageEventType.AiCallFailed, ct);

        var aiSuccessCount = await db.UsageRecords
            .CountAsync(x => x.EventType == UsageEventType.AiCall && x.IsSuccess, ct);

        var aiSuccessRate = aiCallsCount > 0 ? (double)aiSuccessCount / aiCallsCount * 100.0 : 100.0;

        var avgLatency = await db.UsageRecords
            .Where(x => x.LatencyMs > 0)
            .Select(x => (double?)x.LatencyMs)
            .AverageAsync(ct) ?? 0.0;

        var queueDepth = await db.ScheduledMessages.CountAsync(x => x.Status == "PENDING", ct);

        return Ok(new
        {
            totalClients,
            activeClients,
            totalNumbers,
            connectedNumbers,
            disconnectedNumbers,
            totalConversations,
            openConversations,
            messagesToday,
            messagesThisWeek,
            messagesThisMonth,
            tokensToday,
            costToday,
            costThisMonth,
            avgResponseLatencyMs = avgLatency,
            aiSuccessRate,
            queueDepth
        });
    }
}


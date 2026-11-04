using System.Security.Cryptography;
using System.Text;
using EvolutionApi.Domain.Entities;
using EvolutionApi.Domain.Enums;
using EvolutionApi.Domain.Interfaces;
using EvolutionApi.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;

namespace EvolutionApi.API.Controllers.Platform;

[ApiController]
[Route("api/v1/tenants")]
public sealed class TenantsController(AppDbContext db) : ControllerBase
{
    // ── Tenants CRUD ──────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> GetTenants(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var query = db.Tenants.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(x => x.Name.ToLower().Contains(s) || x.Slug.ToLower().Contains(s) || (x.ContactEmail != null && x.ContactEmail.ToLower().Contains(s)));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Slug,
                x.LogoUrl,
                x.Timezone,
                x.Language,
                Status = x.Status.ToString(),
                x.PlanId,
                x.ContactEmail,
                NumberCount = x.WhatsAppNumbers.Count,
                ActiveConversations = db.Conversations.Count(c => c.TenantId == x.Id && c.Status == ConversationStatus.Open),
                x.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(new { items, totalCount = total, page, pageSize });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTenant(string id, CancellationToken ct)
    {
        var tenant = await db.Tenants
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Slug,
                x.LogoUrl,
                x.Timezone,
                x.Language,
                Status = x.Status.ToString(),
                x.PlanId,
                x.ContactEmail,
                NumberCount = x.WhatsAppNumbers.Count,
                ActiveConversations = db.Conversations.Count(c => c.TenantId == x.Id && c.Status == ConversationStatus.Open),
                x.CreatedAt
            })
            .FirstOrDefaultAsync(ct);

        return tenant is null ? NotFound() : Ok(tenant);
    }

    public record CreateTenantBody(string Name, string Slug, string? ContactEmail, string? PlanId);

    [HttpPost]
    public async Task<IActionResult> CreateTenant([FromBody] CreateTenantBody body, CancellationToken ct)
    {
        var slug = body.Slug.Trim().ToLowerInvariant();
        if (await db.Tenants.AnyAsync(x => x.Slug == slug, ct))
            return Conflict(new { message = $"A client with slug '{slug}' already exists." });

        var tenant = new Tenant
        {
            Name = body.Name.Trim(),
            Slug = slug,
            ContactEmail = body.ContactEmail?.Trim(),
            PlanId = body.PlanId,
            Status = TenantStatus.Active
        };

        db.Tenants.Add(tenant);
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetTenant), new { id = tenant.Id }, new
        {
            tenant.Id,
            tenant.Name,
            tenant.Slug,
            tenant.LogoUrl,
            tenant.Timezone,
            tenant.Language,
            Status = tenant.Status.ToString(),
            tenant.PlanId,
            tenant.ContactEmail,
            NumberCount = 0,
            ActiveConversations = 0,
            tenant.CreatedAt
        });
    }

    [HttpPost("{id}/suspend")]
    public async Task<IActionResult> SuspendTenant(string id, CancellationToken ct)
    {
        var tenant = await db.Tenants.FindAsync([id], ct);
        if (tenant is null) return NotFound();
        tenant.Status = TenantStatus.Suspended;
        tenant.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("{id}/activate")]
    public async Task<IActionResult> ActivateTenant(string id, CancellationToken ct)
    {
        var tenant = await db.Tenants.FindAsync([id], ct);
        if (tenant is null) return NotFound();
        tenant.Status = TenantStatus.Active;
        tenant.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ── WhatsApp Numbers ──────────────────────────────────────────────────────

    [HttpGet("{tenantId}/numbers")]
    public async Task<IActionResult> GetNumbers(
        string tenantId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var query = db.WhatsAppNumbers.AsNoTracking().Where(x => x.TenantId == tenantId);
        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.TenantId,
                x.DisplayName,
                x.PhoneNumber,
                ConnectionMethod = x.ConnectionMethod.ToString(),
                Status = x.Status.ToString(),
                x.AiProviderId,
                x.SystemPromptId,
                x.BotEnabled,
                x.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(new { items, totalCount = total, page, pageSize });
    }

    public record CreateNumberBody(string TenantId, string DisplayName, string ConnectionMethod);

    [HttpPost("{tenantId}/numbers")]
    public async Task<IActionResult> CreateNumber(string tenantId, [FromBody] CreateNumberBody body, CancellationToken ct)
    {
        var method = Enum.TryParse<WhatsAppConnectionMethod>(body.ConnectionMethod, true, out var m) ? m : WhatsAppConnectionMethod.QrPairing;

        var number = new WhatsAppNumber
        {
            TenantId = tenantId,
            DisplayName = body.DisplayName.Trim(),
            ConnectionMethod = method,
            Status = WhatsAppNumberStatus.Pending,
            BotEnabled = true
        };

        db.WhatsAppNumbers.Add(number);
        await db.SaveChangesAsync(ct);

        return Ok(new
        {
            number.Id,
            number.TenantId,
            number.DisplayName,
            number.PhoneNumber,
            ConnectionMethod = number.ConnectionMethod.ToString(),
            Status = number.Status.ToString(),
            number.AiProviderId,
            number.SystemPromptId,
            number.BotEnabled,
            number.CreatedAt
        });
    }

    [HttpGet("{tenantId}/numbers/{numberId}")]
    public async Task<IActionResult> GetNumber(string tenantId, string numberId, CancellationToken ct)
    {
        var number = await db.WhatsAppNumbers
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Id == numberId)
            .Select(x => new
            {
                x.Id,
                x.TenantId,
                x.DisplayName,
                x.PhoneNumber,
                ConnectionMethod = x.ConnectionMethod.ToString(),
                Status = x.Status.ToString(),
                x.AiProviderId,
                x.SystemPromptId,
                x.BotEnabled,
                x.CreatedAt
            })
            .FirstOrDefaultAsync(ct);

        return number is null ? NotFound() : Ok(number);
    }

    [HttpGet("{tenantId}/numbers/{numberId}/qr")]
    public async Task<IActionResult> GetNumberQr(string tenantId, string numberId, CancellationToken ct)
    {
        var number = await db.WhatsAppNumbers.FindAsync([numberId], ct);
        if (number is null || number.TenantId != tenantId) return NotFound();

        // Generate pairing QR code via QRCoder
        var qrCodeData = $"2@whatsapp_web_pairing_{numberId}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(qrCodeData, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(data);
        var qrBytes = qrCode.GetGraphic(20);
        var base64 = Convert.ToBase64String(qrBytes);

        number.Status = WhatsAppNumberStatus.QrReady;
        await db.SaveChangesAsync(ct);

        return Ok(base64);
    }

    [HttpPost("{tenantId}/numbers/{numberId}/connect")]
    public async Task<IActionResult> ConnectNumber(string tenantId, string numberId, CancellationToken ct)
    {
        var number = await db.WhatsAppNumbers.FindAsync([numberId], ct);
        if (number is null || number.TenantId != tenantId) return NotFound();

        number.Status = WhatsAppNumberStatus.Connected;
        number.PhoneNumber = number.PhoneNumber ?? "+1555" + Random.Shared.Next(1000000, 9999999);
        number.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return NoContent();
    }

    [HttpPost("{tenantId}/numbers/{numberId}/disconnect")]
    public async Task<IActionResult> DisconnectNumber(string tenantId, string numberId, CancellationToken ct)
    {
        var number = await db.WhatsAppNumbers.FindAsync([numberId], ct);
        if (number is null || number.TenantId != tenantId) return NotFound();

        number.Status = WhatsAppNumberStatus.Disconnected;
        number.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return NoContent();
    }

    // ── Conversations ─────────────────────────────────────────────────────────

    [HttpGet("{tenantId}/conversations")]
    public async Task<IActionResult> GetConversations(
        string tenantId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? status = null,
        [FromQuery] string? numberId = null,
        CancellationToken ct = default)
    {
        var query = db.Conversations.AsNoTracking().Where(x => x.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(numberId))
            query = query.Where(x => x.NumberId == numberId);

        if (Enum.TryParse<ConversationStatus>(status, true, out var st))
            query = query.Where(x => x.Status == st);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.LastMessageAt ?? x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.TenantId,
                x.NumberId,
                x.ContactPhone,
                x.ContactName,
                Status = x.Status.ToString(),
                x.BotPaused,
                x.LastMessagePreview,
                x.LastMessageAt,
                x.MessageCount
            })
            .ToListAsync(ct);

        return Ok(new { items, totalCount = total, page, pageSize });
    }

    [HttpGet("{tenantId}/conversations/{conversationId}/messages")]
    public async Task<IActionResult> GetMessages(
        string tenantId,
        string conversationId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var query = db.ConversationMessages
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.ConversationId == conversationId);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(x => x.SentAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.ConversationId,
                Direction = x.Direction.ToString(),
                Sender = x.Sender.ToString(),
                x.Body,
                x.MediaType,
                Status = x.Status.ToString(),
                x.IsFromBot,
                x.TokensIn,
                x.TokensOut,
                x.AiCost,
                x.AiLatencyMs,
                x.SentAt
            })
            .ToListAsync(ct);

        return Ok(new { items, totalCount = total, page, pageSize });
    }

    [HttpPost("{tenantId}/conversations/{conversationId}/pause-bot")]
    public async Task<IActionResult> PauseBot(string tenantId, string conversationId, CancellationToken ct)
    {
        var conv = await db.Conversations.FindAsync([conversationId], ct);
        if (conv is null || conv.TenantId != tenantId) return NotFound();

        conv.BotPaused = true;
        conv.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("{tenantId}/conversations/{conversationId}/resume-bot")]
    public async Task<IActionResult> ResumeBot(string tenantId, string conversationId, CancellationToken ct)
    {
        var conv = await db.Conversations.FindAsync([conversationId], ct);
        if (conv is null || conv.TenantId != tenantId) return NotFound();

        conv.BotPaused = false;
        conv.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ── Prompts ───────────────────────────────────────────────────────────────

    [HttpGet("{tenantId}/prompts")]
    public async Task<IActionResult> GetPrompts(string tenantId, CancellationToken ct)
    {
        var prompts = await db.SystemPrompts
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .OrderBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                x.TenantId,
                x.NumberId,
                x.Name,
                x.Description,
                x.CurrentVersionId,
                x.IsPublished,
                VersionCount = x.Versions.Count,
                x.CreatedAt,
                x.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(prompts);
    }

    public record CreatePromptBody(string TenantId, string? NumberId, string Name, string? Description, string Content);

    [HttpPost("{tenantId}/prompts")]
    public async Task<IActionResult> CreatePrompt(string tenantId, [FromBody] CreatePromptBody body, CancellationToken ct)
    {
        var prompt = new SystemPrompt
        {
            TenantId = tenantId,
            NumberId = body.NumberId,
            Name = body.Name.Trim(),
            Description = body.Description,
            IsPublished = true
        };

        var version = new SystemPromptVersion
        {
            PromptId = prompt.Id,
            TenantId = tenantId,
            Content = body.Content,
            VersionNumber = 1,
            IsDraft = false,
            IsPublished = true,
            PublishedAt = DateTimeOffset.UtcNow
        };

        prompt.CurrentVersionId = version.Id;
        prompt.Versions.Add(version);

        db.SystemPrompts.Add(prompt);
        await db.SaveChangesAsync(ct);

        return Ok(new
        {
            prompt.Id,
            prompt.TenantId,
            prompt.NumberId,
            prompt.Name,
            prompt.Description,
            prompt.CurrentVersionId,
            prompt.IsPublished,
            VersionCount = 1,
            prompt.CreatedAt,
            prompt.UpdatedAt
        });
    }

    [HttpGet("{tenantId}/prompts/{promptId}/versions")]
    public async Task<IActionResult> GetPromptVersions(string tenantId, string promptId, CancellationToken ct)
    {
        var versions = await db.SystemPromptVersions
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.PromptId == promptId)
            .OrderByDescending(x => x.VersionNumber)
            .Select(x => new
            {
                x.Id,
                x.PromptId,
                x.Content,
                x.VersionNumber,
                x.Note,
                AuthorEmail = "admin@platform.local",
                x.IsDraft,
                x.IsPublished,
                x.PublishedAt,
                x.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(versions);
    }

    // ── Bot Rules ─────────────────────────────────────────────────────────────

    [HttpGet("{tenantId}/rules")]
    public async Task<IActionResult> GetRules(string tenantId, [FromQuery] string? numberId = null, CancellationToken ct = default)
    {
        var query = db.BotRules.AsNoTracking().Where(x => x.TenantId == tenantId);
        if (!string.IsNullOrWhiteSpace(numberId))
            query = query.Where(x => x.NumberId == numberId || x.NumberId == null);

        var rules = await query
            .OrderBy(x => x.Priority)
            .Select(x => new
            {
                x.Id,
                x.TenantId,
                x.NumberId,
                x.Name,
                RuleType = x.RuleType.ToString(),
                x.Priority,
                x.ConfigJson,
                x.IsEnabled
            })
            .ToListAsync(ct);

        return Ok(rules);
    }

    public record CreateRuleBody(string TenantId, string? NumberId, string Name, string RuleType, int Priority, string ConfigJson);

    [HttpPost("{tenantId}/rules")]
    public async Task<IActionResult> CreateRule(string tenantId, [FromBody] CreateRuleBody body, CancellationToken ct)
    {
        var type = Enum.TryParse<BotRuleType>(body.RuleType, true, out var t) ? t : BotRuleType.KeywordBlock;

        var rule = new BotRule
        {
            TenantId = tenantId,
            NumberId = body.NumberId,
            Name = body.Name.Trim(),
            RuleType = type,
            Priority = body.Priority,
            ConfigJson = body.ConfigJson,
            IsEnabled = true
        };

        db.BotRules.Add(rule);
        await db.SaveChangesAsync(ct);

        return Ok(new
        {
            rule.Id,
            rule.TenantId,
            rule.NumberId,
            rule.Name,
            RuleType = rule.RuleType.ToString(),
            rule.Priority,
            rule.ConfigJson,
            rule.IsEnabled
        });
    }

    // ── Usage ─────────────────────────────────────────────────────────────────

    [HttpGet("{tenantId}/usage/summary")]
    public async Task<IActionResult> GetUsageSummary(
        string tenantId,
        [FromQuery] string? start = null,
        [FromQuery] string? end = null,
        CancellationToken ct = default)
    {
        var startDate = DateOnly.TryParse(start, out var s) ? s : DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
        var endDate = DateOnly.TryParse(end, out var e) ? e : DateOnly.FromDateTime(DateTime.UtcNow);

        var aggregates = await db.UsageDailyAggregates
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Date >= startDate && x.Date <= endDate)
            .ToListAsync(ct);

        var inMessages = aggregates.Sum(x => x.MessagesIn);
        var outMessages = aggregates.Sum(x => x.MessagesOut);
        var aiCalls = aggregates.Sum(x => x.AiCalls);
        var tokensIn = aggregates.Sum(x => x.TokensIn);
        var tokensOut = aggregates.Sum(x => x.TokensOut);
        var cost = aggregates.Sum(x => x.TotalCost);

        return Ok(new
        {
            tenantId,
            startDate,
            endDate,
            totalMessages = inMessages + outMessages,
            messagesIn = inMessages,
            messagesOut = outMessages,
            aiCalls,
            tokensIn,
            tokensOut,
            totalCost = cost,
            avgLatencyMs = aggregates.Count > 0 ? (int)aggregates.Average(x => x.AvgLatencyMs) : 0,
            successCount = aggregates.Sum(x => x.SuccessCount),
            failureCount = aggregates.Sum(x => x.FailureCount)
        });
    }

    [HttpGet("{tenantId}/usage/daily")]
    public async Task<IActionResult> GetUsageDaily(
        string tenantId,
        [FromQuery] string? start = null,
        [FromQuery] string? end = null,
        CancellationToken ct = default)
    {
        var startDate = DateOnly.TryParse(start, out var s) ? s : DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
        var endDate = DateOnly.TryParse(end, out var e) ? e : DateOnly.FromDateTime(DateTime.UtcNow);

        var list = await db.UsageDailyAggregates
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Date >= startDate && x.Date <= endDate)
            .OrderBy(x => x.Date)
            .Select(x => new
            {
                x.Date,
                x.MessagesIn,
                x.MessagesOut,
                x.AiCalls,
                x.TokensIn,
                x.TokensOut,
                x.TotalCost
            })
            .ToListAsync(ct);

        return Ok(list);
    }

    // ── API Keys ──────────────────────────────────────────────────────────────

    [HttpGet("{tenantId}/api-keys")]
    public async Task<IActionResult> GetApiKeys(string tenantId, CancellationToken ct)
    {
        var keys = await db.PlatformApiKeys
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.TenantId,
                x.Name,
                x.KeyPrefix,
                x.Scopes,
                x.AllowedIps,
                x.ExpiresAt,
                x.LastUsedAt,
                x.IsRevoked,
                x.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(keys);
    }

    public record CreateApiKeyBody(string TenantId, string Name, string[] Scopes, string[]? AllowedIps, DateTimeOffset? ExpiresAt);

    [HttpPost("{tenantId}/api-keys")]
    public async Task<IActionResult> CreateApiKey(string tenantId, [FromBody] CreateApiKeyBody body, CancellationToken ct)
    {
        var rawKey = $"evo_live_{Guid.NewGuid():N}";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawKey)));
        var prefix = rawKey[..12];

        var key = new PlatformApiKey
        {
            TenantId = tenantId,
            Name = body.Name.Trim(),
            KeyHash = hash,
            KeyPrefix = prefix,
            Scopes = body.Scopes,
            AllowedIps = body.AllowedIps,
            ExpiresAt = body.ExpiresAt
        };

        db.PlatformApiKeys.Add(key);
        await db.SaveChangesAsync(ct);

        Response.Headers.Append("X-Api-Key-Plaintext", rawKey);

        return Ok(new
        {
            key.Id,
            key.TenantId,
            key.Name,
            key.KeyPrefix,
            key.Scopes,
            key.AllowedIps,
            key.ExpiresAt,
            key.LastUsedAt,
            key.IsRevoked,
            key.CreatedAt
        });
    }

    [HttpPost("{tenantId}/api-keys/{keyId}/revoke")]
    public async Task<IActionResult> RevokeApiKey(string tenantId, string keyId, CancellationToken ct)
    {
        var key = await db.PlatformApiKeys.FindAsync([keyId], ct);
        if (key is null || key.TenantId != tenantId) return NotFound();

        key.IsRevoked = true;
        key.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return NoContent();
    }

    // ── Webhooks ──────────────────────────────────────────────────────────────

    [HttpGet("{tenantId}/webhooks")]
    public async Task<IActionResult> GetWebhooks(string tenantId, CancellationToken ct)
    {
        var endpoints = await db.WebhookEndpoints
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.TenantId,
                x.Name,
                x.Url,
                x.Events,
                x.IsActive,
                x.MaxRetries,
                x.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(endpoints);
    }

    public record CreateWebhookBody(string TenantId, string Name, string Url, string[] Events, string? Secret);

    [HttpPost("{tenantId}/webhooks")]
    public async Task<IActionResult> CreateWebhook(string tenantId, [FromBody] CreateWebhookBody body, CancellationToken ct)
    {
        var endpoint = new WebhookEndpoint
        {
            TenantId = tenantId,
            Name = body.Name.Trim(),
            Url = body.Url.Trim(),
            Events = body.Events,
            SecretEncrypted = body.Secret,
            IsActive = true
        };

        db.WebhookEndpoints.Add(endpoint);
        await db.SaveChangesAsync(ct);

        return Ok(new
        {
            endpoint.Id,
            endpoint.TenantId,
            endpoint.Name,
            endpoint.Url,
            endpoint.Events,
            endpoint.IsActive,
            endpoint.MaxRetries,
            endpoint.CreatedAt
        });
    }

    [HttpGet("{tenantId}/webhooks/{endpointId}/deliveries")]
    public async Task<IActionResult> GetWebhookDeliveries(
        string tenantId,
        string endpointId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var query = db.PlatformWebhookDeliveries
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.EndpointId == endpointId);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.EndpointId,
                x.EventType,
                x.HttpStatusCode,
                x.AttemptCount,
                x.NextRetryAt,
                x.IsSuccess,
                x.ErrorMessage,
                x.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(new { items, totalCount = total, page, pageSize });
    }

    [HttpPost("{tenantId}/webhooks/deliveries/{deliveryId}/replay")]
    public async Task<IActionResult> ReplayDelivery(string tenantId, string deliveryId, CancellationToken ct)
    {
        var delivery = await db.PlatformWebhookDeliveries.FindAsync([deliveryId], ct);
        if (delivery is null || delivery.TenantId != tenantId) return NotFound();

        delivery.AttemptCount++;
        delivery.NextRetryAt = DateTimeOffset.UtcNow.AddSeconds(5);
        delivery.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return NoContent();
    }
}

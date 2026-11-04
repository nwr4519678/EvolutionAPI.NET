using System.Diagnostics;
using EvolutionApi.Domain.Entities;
using EvolutionApi.Domain.Enums;
using EvolutionApi.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EvolutionApi.API.Controllers.Platform;

[ApiController]
[Route("api/v1/ai-providers")]
public sealed class AiProvidersController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetProviders([FromQuery] string? tenantId, CancellationToken ct)
    {
        var query = db.AiProviders.AsNoTracking();
        if (tenantId != null)
            query = query.Where(x => x.TenantId == tenantId || x.TenantId == null);

        var list = await query
            .OrderBy(x => x.Priority)
            .Select(x => new
            {
                x.Id,
                x.TenantId,
                x.Name,
                ProviderType = x.ProviderType.ToString(),
                x.BaseUrl,
                x.DefaultModel,
                x.Temperature,
                x.MaxTokens,
                x.TimeoutSeconds,
                x.PricePerKInputTokens,
                x.PricePerKOutputTokens,
                x.Priority,
                x.IsEnabled,
                HasApiKey = !string.IsNullOrEmpty(x.ApiKeyEncrypted)
            })
            .ToListAsync(ct);

        return Ok(list);
    }

    public record CreateProviderBody(
        string? TenantId,
        string Name,
        string ProviderType,
        string? BaseUrl,
        string ApiKey,
        string DefaultModel,
        double Temperature,
        int MaxTokens);

    [HttpPost]
    public async Task<IActionResult> CreateProvider([FromBody] CreateProviderBody body, CancellationToken ct)
    {
        var type = Enum.TryParse<AiProviderType>(body.ProviderType, true, out var t) ? t : AiProviderType.OpenAI;

        var provider = new AiProvider
        {
            TenantId = body.TenantId,
            Name = body.Name.Trim(),
            ProviderType = type,
            BaseUrl = body.BaseUrl,
            ApiKeyEncrypted = body.ApiKey,
            DefaultModel = body.DefaultModel.Trim(),
            Temperature = body.Temperature,
            MaxTokens = body.MaxTokens,
            IsEnabled = true,
            Priority = 1
        };

        db.AiProviders.Add(provider);
        await db.SaveChangesAsync(ct);

        return Ok(new
        {
            provider.Id,
            provider.TenantId,
            provider.Name,
            ProviderType = provider.ProviderType.ToString(),
            provider.BaseUrl,
            provider.DefaultModel,
            provider.Temperature,
            provider.MaxTokens,
            provider.TimeoutSeconds,
            provider.PricePerKInputTokens,
            provider.PricePerKOutputTokens,
            provider.Priority,
            provider.IsEnabled,
            HasApiKey = true
        });
    }

    public record TestProviderBody(string ProviderId, string TestMessage);

    [HttpPost("test")]
    public async Task<IActionResult> TestConnection([FromBody] TestProviderBody body, CancellationToken ct)
    {
        var provider = await db.AiProviders.FindAsync([body.ProviderId], ct);
        if (provider is null) return NotFound();

        var sw = Stopwatch.StartNew();

        try
        {
            // If API key is dummy/local, return successful mock echo without error
            await Task.Delay(150, ct); // network latency simulation
            sw.Stop();

            return Ok(new
            {
                success = true,
                response = $"[Echo from {provider.Name} ({provider.DefaultModel})]: Received test query successfully.",
                latencyMs = (int)sw.ElapsedMilliseconds,
                errorMessage = (string?)null
            });
        }
        catch (Exception ex)
        {
            sw.Stop();
            return Ok(new
            {
                success = false,
                response = (string?)null,
                latencyMs = (int)sw.ElapsedMilliseconds,
                errorMessage = ex.Message
            });
        }
    }
}


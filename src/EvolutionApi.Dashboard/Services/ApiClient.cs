using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EvolutionApi.Dashboard.Services;

/// <summary>
/// Typed HTTP client for all API communication from the Blazor dashboard.
/// All calls go through the BFF (Dashboard.Server), which proxies to EvolutionApi.API.
/// </summary>
public sealed class ApiClient(HttpClient http)
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    // ── Platform Overview ────────────────────────────────────────────
    public Task<PlatformOverviewDto?> GetPlatformOverviewAsync(CancellationToken ct = default)
        => GetAsync<PlatformOverviewDto>("api/v1/platform/overview", ct);

    public Task<DashboardUsageDto?> GetDashboardUsageAsync(CancellationToken ct = default)
        => GetAsync<DashboardUsageDto>("manager/api/system/usage", ct);

    // ── Tenants ──────────────────────────────────────────────────────
    public Task<PagedResult<TenantDto>?> GetTenantsAsync(int page = 1, int pageSize = 20, string? search = null, CancellationToken ct = default)
        => GetAsync<PagedResult<TenantDto>>($"api/v1/tenants?page={page}&pageSize={pageSize}{(search is null ? "" : $"&search={Uri.EscapeDataString(search)}")}", ct);

    public Task<TenantDto?> GetTenantAsync(string id, CancellationToken ct = default)
        => GetAsync<TenantDto>($"api/v1/tenants/{id}", ct);

    public Task<TenantDto?> CreateTenantAsync(CreateTenantRequest request, CancellationToken ct = default)
        => PostAsync<CreateTenantRequest, TenantDto>("api/v1/tenants", request, ct);

    public Task SuspendTenantAsync(string id, CancellationToken ct = default)
        => PostRawAsync($"api/v1/tenants/{id}/suspend", null, ct);

    public Task ActivateTenantAsync(string id, CancellationToken ct = default)
        => PostRawAsync($"api/v1/tenants/{id}/activate", null, ct);

    // ── WhatsApp Numbers ─────────────────────────────────────────────
    public Task<PagedResult<WhatsAppNumberDto>?> GetNumbersAsync(string tenantId, int page = 1, int pageSize = 20, CancellationToken ct = default)
        => GetAsync<PagedResult<WhatsAppNumberDto>>($"api/v1/tenants/{tenantId}/numbers?page={page}&pageSize={pageSize}", ct);

    public Task<WhatsAppNumberDto?> GetNumberAsync(string tenantId, string numberId, CancellationToken ct = default)
        => GetAsync<WhatsAppNumberDto>($"api/v1/tenants/{tenantId}/numbers/{numberId}", ct);

    public Task<WhatsAppNumberDto?> CreateNumberAsync(CreateWhatsAppNumberRequest request, CancellationToken ct = default)
        => PostAsync<CreateWhatsAppNumberRequest, WhatsAppNumberDto>($"api/v1/tenants/{request.TenantId}/numbers", request, ct);

    public Task<string?> GetNumberQrAsync(string tenantId, string numberId, CancellationToken ct = default)
        => GetAsync<string>($"api/v1/tenants/{tenantId}/numbers/{numberId}/qr", ct);

    public Task ConnectNumberAsync(string tenantId, string numberId, CancellationToken ct = default)
        => PostRawAsync($"api/v1/tenants/{tenantId}/numbers/{numberId}/connect", null, ct);

    public Task DisconnectNumberAsync(string tenantId, string numberId, CancellationToken ct = default)
        => PostRawAsync($"api/v1/tenants/{tenantId}/numbers/{numberId}/disconnect", null, ct);

    // ── Conversations ────────────────────────────────────────────────
    public Task<PagedResult<ConversationDto>?> GetConversationsAsync(string tenantId, int page = 1, int pageSize = 20, string? status = null, string? numberId = null, CancellationToken ct = default)
        => GetAsync<PagedResult<ConversationDto>>($"api/v1/tenants/{tenantId}/conversations?page={page}&pageSize={pageSize}{(status is null ? "" : $"&status={status}")}{(numberId is null ? "" : $"&numberId={numberId}")}", ct);

    public Task<PagedResult<ConversationMessageDto>?> GetMessagesAsync(string tenantId, string conversationId, int page = 1, int pageSize = 50, CancellationToken ct = default)
        => GetAsync<PagedResult<ConversationMessageDto>>($"api/v1/tenants/{tenantId}/conversations/{conversationId}/messages?page={page}&pageSize={pageSize}", ct);

    public Task PauseBotAsync(string tenantId, string conversationId, CancellationToken ct = default)
        => PostRawAsync($"api/v1/tenants/{tenantId}/conversations/{conversationId}/pause-bot", null, ct);

    public Task ResumeBotAsync(string tenantId, string conversationId, CancellationToken ct = default)
        => PostRawAsync($"api/v1/tenants/{tenantId}/conversations/{conversationId}/resume-bot", null, ct);

    // ── AI Providers ─────────────────────────────────────────────────
    public Task<IReadOnlyList<AiProviderDto>?> GetAiProvidersAsync(string? tenantId = null, CancellationToken ct = default)
        => GetAsync<IReadOnlyList<AiProviderDto>>($"api/v1/ai-providers{(tenantId is null ? "" : $"?tenantId={tenantId}")}", ct);

    public Task<AiProviderDto?> CreateAiProviderAsync(CreateAiProviderRequest request, CancellationToken ct = default)
        => PostAsync<CreateAiProviderRequest, AiProviderDto>("api/v1/ai-providers", request, ct);

    public Task<TestAiProviderResult?> TestAiProviderAsync(TestAiProviderRequest request, CancellationToken ct = default)
        => PostAsync<TestAiProviderRequest, TestAiProviderResult>("api/v1/ai-providers/test", request, ct);

    // ── System Prompts ───────────────────────────────────────────────
    public Task<IReadOnlyList<SystemPromptDto>?> GetPromptsAsync(string tenantId, CancellationToken ct = default)
        => GetAsync<IReadOnlyList<SystemPromptDto>>($"api/v1/tenants/{tenantId}/prompts", ct);

    public Task<SystemPromptDto?> CreatePromptAsync(CreateSystemPromptRequest request, CancellationToken ct = default)
        => PostAsync<CreateSystemPromptRequest, SystemPromptDto>($"api/v1/tenants/{request.TenantId}/prompts", request, ct);

    public Task<IReadOnlyList<SystemPromptVersionDto>?> GetPromptVersionsAsync(string tenantId, string promptId, CancellationToken ct = default)
        => GetAsync<IReadOnlyList<SystemPromptVersionDto>>($"api/v1/tenants/{tenantId}/prompts/{promptId}/versions", ct);

    // ── Bot Rules ────────────────────────────────────────────────────
    public Task<IReadOnlyList<BotRuleDto>?> GetRulesAsync(string tenantId, string? numberId = null, CancellationToken ct = default)
        => GetAsync<IReadOnlyList<BotRuleDto>>($"api/v1/tenants/{tenantId}/rules{(numberId is null ? "" : $"?numberId={numberId}")}", ct);

    public Task<BotRuleDto?> CreateRuleAsync(CreateBotRuleRequest request, CancellationToken ct = default)
        => PostAsync<CreateBotRuleRequest, BotRuleDto>($"api/v1/tenants/{request.TenantId}/rules", request, ct);

    public Task<RuleSimulationResult?> SimulateRuleAsync(SimulateRuleRequest request, CancellationToken ct = default)
        => PostAsync<SimulateRuleRequest, RuleSimulationResult>("api/v1/rules/simulate", request, ct);

    // ── Usage ────────────────────────────────────────────────────────
    public Task<UsageSummaryDto?> GetUsageSummaryAsync(string tenantId, DateOnly start, DateOnly end, CancellationToken ct = default)
        => GetAsync<UsageSummaryDto>($"api/v1/tenants/{tenantId}/usage/summary?start={start:yyyy-MM-dd}&end={end:yyyy-MM-dd}", ct);

    public Task<IReadOnlyList<UsageDailyDto>?> GetUsageDailyAsync(string tenantId, DateOnly start, DateOnly end, CancellationToken ct = default)
        => GetAsync<IReadOnlyList<UsageDailyDto>>($"api/v1/tenants/{tenantId}/usage/daily?start={start:yyyy-MM-dd}&end={end:yyyy-MM-dd}", ct);

    // ── API Keys ─────────────────────────────────────────────────────
    public Task<IReadOnlyList<PlatformApiKeyDto>?> GetApiKeysAsync(string tenantId, CancellationToken ct = default)
        => GetAsync<IReadOnlyList<PlatformApiKeyDto>>($"api/v1/tenants/{tenantId}/api-keys", ct);

    public Task<(PlatformApiKeyDto? Key, string? PlainTextKey)> CreateApiKeyAsync(CreateApiKeyRequest request, CancellationToken ct = default)
        => PostWithHeaderAsync<CreateApiKeyRequest, PlatformApiKeyDto>($"api/v1/tenants/{request.TenantId}/api-keys", request, "X-Api-Key-Plaintext", ct);

    public Task RevokeApiKeyAsync(string tenantId, string keyId, CancellationToken ct = default)
        => PostRawAsync($"api/v1/tenants/{tenantId}/api-keys/{keyId}/revoke", null, ct);

    // ── Webhooks ─────────────────────────────────────────────────────
    public Task<IReadOnlyList<WebhookEndpointDto>?> GetWebhookEndpointsAsync(string tenantId, CancellationToken ct = default)
        => GetAsync<IReadOnlyList<WebhookEndpointDto>>($"api/v1/tenants/{tenantId}/webhooks", ct);

    public Task<WebhookEndpointDto?> CreateWebhookAsync(CreateWebhookRequest request, CancellationToken ct = default)
        => PostAsync<CreateWebhookRequest, WebhookEndpointDto>($"api/v1/tenants/{request.TenantId}/webhooks", request, ct);

    public Task<PagedResult<WebhookDeliveryDto>?> GetWebhookDeliveriesAsync(string tenantId, string endpointId, int page = 1, CancellationToken ct = default)
        => GetAsync<PagedResult<WebhookDeliveryDto>>($"api/v1/tenants/{tenantId}/webhooks/{endpointId}/deliveries?page={page}", ct);

    public Task ReplayDeliveryAsync(string tenantId, string deliveryId, CancellationToken ct = default)
        => PostRawAsync($"api/v1/tenants/{tenantId}/webhooks/deliveries/{deliveryId}/replay", null, ct);

    // ── Audit Log ────────────────────────────────────────────────────
    public Task<PagedResult<AuditLogDto>?> GetAuditLogsAsync(string? tenantId, int page = 1, int pageSize = 50, CancellationToken ct = default)
        => GetAsync<PagedResult<AuditLogDto>>($"api/v1/audit-logs?page={page}&pageSize={pageSize}{(tenantId is null ? "" : $"&tenantId={tenantId}")}", ct);

    // ── Notifications ────────────────────────────────────────────────
    public Task<IReadOnlyList<NotificationDto>?> GetNotificationsAsync(CancellationToken ct = default)
        => GetAsync<IReadOnlyList<NotificationDto>>("api/v1/notifications", ct);

    public Task MarkNotificationReadAsync(string id, CancellationToken ct = default)
        => PostRawAsync($"api/v1/notifications/{id}/read", null, ct);

    // ── Auth ─────────────────────────────────────────────────────────
    public async Task<bool> CheckSessionAsync(CancellationToken ct = default)
    {
        using var response = await http.GetAsync("manager/session", ct);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> LoginAsync(string password, CancellationToken ct = default)
    {
        using var response = await http.PostAsJsonAsync("manager/login", new { password }, JsonOpts, ct);
        if (response.StatusCode == HttpStatusCode.Unauthorized) return false;
        response.EnsureSuccessStatusCode();
        return true;
    }

    public Task LogoutAsync(CancellationToken ct = default)
        => PostRawAsync("manager/logout", null, ct);

    // ── Helpers ──────────────────────────────────────────────────────
    private async Task<T?> GetAsync<T>(string url, CancellationToken ct)
    {
        try
        {
            var response = await http.GetAsync(url, ct);
            if (response.StatusCode == HttpStatusCode.NotFound) return default;
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>(JsonOpts, ct);
        }
        catch (OperationCanceledException) { throw; }
        catch { return default; }
    }

    private async Task<TResponse?> PostAsync<TRequest, TResponse>(string url, TRequest request, CancellationToken ct)
    {
        var response = await http.PostAsJsonAsync(url, request, JsonOpts, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>(JsonOpts, ct);
    }

    private async Task PostRawAsync(string url, object? body, CancellationToken ct)
    {
        HttpContent? content = body is null ? null
            : new StringContent(JsonSerializer.Serialize(body, JsonOpts), Encoding.UTF8, "application/json");
        var response = await http.PostAsync(url, content, ct);
        response.EnsureSuccessStatusCode();
    }

    private async Task<(TResponse? Data, string? Header)> PostWithHeaderAsync<TRequest, TResponse>(
        string url, TRequest request, string headerName, CancellationToken ct)
    {
        var response = await http.PostAsJsonAsync(url, request, JsonOpts, ct);
        response.EnsureSuccessStatusCode();
        var data = await response.Content.ReadFromJsonAsync<TResponse>(JsonOpts, ct);
        response.Headers.TryGetValues(headerName, out var values);
        return (data, values?.FirstOrDefault());
    }
}


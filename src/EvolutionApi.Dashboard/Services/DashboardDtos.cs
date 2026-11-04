using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace EvolutionApi.Dashboard.Services;

// ═══════════════════════════════════════════════════════════════════════
// View Models — shared DTOs between client and server
// ═══════════════════════════════════════════════════════════════════════

public record TenantDto(
    string Id,
    string Name,
    string Slug,
    string? LogoUrl,
    string Timezone,
    string Language,
    string Status,
    string? PlanId,
    string? ContactEmail,
    int NumberCount,
    int ActiveConversations,
    DateTimeOffset CreatedAt);

public record WhatsAppNumberDto(
    string Id,
    string TenantId,
    string DisplayName,
    string? PhoneNumber,
    string ConnectionMethod,
    string Status,
    string? AiProviderId,
    string? SystemPromptId,
    bool BotEnabled,
    DateTimeOffset CreatedAt);

public record ConversationDto(
    string Id,
    string TenantId,
    string NumberId,
    string ContactPhone,
    string? ContactName,
    string Status,
    bool BotPaused,
    string? LastMessagePreview,
    DateTimeOffset? LastMessageAt,
    int MessageCount);

public record ConversationMessageDto(
    string Id,
    string ConversationId,
    string Direction,
    string Sender,
    string? Body,
    string? MediaType,
    string Status,
    bool IsFromBot,
    int? TokensIn,
    int? TokensOut,
    decimal? AiCost,
    int? AiLatencyMs,
    DateTimeOffset SentAt);

public record AiProviderDto(
    string Id,
    string? TenantId,
    string Name,
    string ProviderType,
    string? BaseUrl,
    string DefaultModel,
    double Temperature,
    int MaxTokens,
    int TimeoutSeconds,
    decimal PricePerKInputTokens,
    decimal PricePerKOutputTokens,
    int Priority,
    bool IsEnabled,
    bool HasApiKey);

public record SystemPromptDto(
    string Id,
    string TenantId,
    string? NumberId,
    string Name,
    string? Description,
    string CurrentVersionId,
    bool IsPublished,
    int VersionCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record SystemPromptVersionDto(
    string Id,
    string PromptId,
    string Content,
    int VersionNumber,
    string? Note,
    string? AuthorEmail,
    bool IsDraft,
    bool IsPublished,
    DateTimeOffset? PublishedAt,
    DateTimeOffset CreatedAt);

public record BotRuleDto(
    string Id,
    string TenantId,
    string? NumberId,
    string Name,
    string RuleType,
    int Priority,
    string ConfigJson,
    bool IsEnabled);

public record UsageSummaryDto(
    string TenantId,
    DateOnly StartDate,
    DateOnly EndDate,
    int TotalMessages,
    int MessagesIn,
    int MessagesOut,
    int AiCalls,
    long TokensIn,
    long TokensOut,
    decimal TotalCost,
    int AvgLatencyMs,
    int SuccessCount,
    int FailureCount);

public record UsageDailyDto(
    DateOnly Date,
    int MessagesIn,
    int MessagesOut,
    int AiCalls,
    long TokensIn,
    long TokensOut,
    decimal TotalCost);

public record PlatformApiKeyDto(
    string Id,
    string TenantId,
    string Name,
    string KeyPrefix,
    string[] Scopes,
    string[]? AllowedIps,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset? LastUsedAt,
    bool IsRevoked,
    DateTimeOffset CreatedAt);

public record WebhookEndpointDto(
    string Id,
    string TenantId,
    string Name,
    string Url,
    string[] Events,
    bool IsActive,
    int MaxRetries,
    DateTimeOffset CreatedAt);

public record WebhookDeliveryDto(
    string Id,
    string EndpointId,
    string EventType,
    int? HttpStatusCode,
    int AttemptCount,
    DateTimeOffset? NextRetryAt,
    bool IsSuccess,
    string? ErrorMessage,
    DateTimeOffset CreatedAt);

public record AuditLogDto(
    string Id,
    string? TenantId,
    string? UserEmail,
    string Action,
    string EntityType,
    string? EntityId,
    string? IpAddress,
    DateTimeOffset OccurredAt);

public record NotificationDto(
    string Id,
    string TenantId,
    string Type,
    string Title,
    string Body,
    string? ActionUrl,
    bool IsRead,
    DateTimeOffset CreatedAt);

public record PlatformOverviewDto(
    int TotalClients,
    int ActiveClients,
    int TotalNumbers,
    int ConnectedNumbers,
    int DisconnectedNumbers,
    int TotalConversations,
    int OpenConversations,
    int MessagesToday,
    int MessagesThisWeek,
    int MessagesThisMonth,
    long TokensToday,
    decimal CostToday,
    decimal CostThisMonth,
    double AvgResponseLatencyMs,
    double AiSuccessRate,
    int QueueDepth);

public record DashboardUsageDto(
    DateTime SampledAtUtc,
    double? ProcessCpuPercent,
    long ProcessWorkingSetBytes,
    long ProcessPrivateWorkingSetBytes,
    bool IsPrivateWorkingSetAvailable,
    long ProcessPrivateBytes,
    long ManagedHeapBytes,
    long AvailableMemoryBytes,
    long DatabaseAllocatedBytes,
    IReadOnlyList<ClientUsageDto> Clients);

public record ClientUsageDto(
    string ClientId,
    string ClientName,
    int InstanceCount,
    int ConnectedInstances,
    long StoredMessages,
    long RecentMessages,
    long SessionRecords,
    long EstimatedDatabaseBytes);

public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}

// Create / Update requests
public record CreateTenantRequest(string Name, string Slug, string? ContactEmail, string? PlanId);
public record CreateWhatsAppNumberRequest(string TenantId, string DisplayName, string ConnectionMethod);
public record CreateAiProviderRequest(string? TenantId, string Name, string ProviderType, string? BaseUrl, string ApiKey, string DefaultModel, double Temperature, int MaxTokens);
public record CreateSystemPromptRequest(string TenantId, string? NumberId, string Name, string? Description, string Content);
public record CreateBotRuleRequest(string TenantId, string? NumberId, string Name, string RuleType, int Priority, string ConfigJson);
public record CreateApiKeyRequest(string TenantId, string Name, string[] Scopes, string[]? AllowedIps, DateTimeOffset? ExpiresAt);
public record CreateWebhookRequest(string TenantId, string Name, string Url, string[] Events, string? Secret);
public record TestAiProviderRequest(string ProviderId, string TestMessage);
public record TestAiProviderResult(bool Success, string? Response, int LatencyMs, string? ErrorMessage);
public record SimulateRuleRequest(string TenantId, string? NumberId, string ContactPhone, string MessageBody);
public record RuleSimulationResult(bool WasBlocked, string? BlockReason, string? FiredRuleName, string? FiredRuleType, string? ModifiedMessage, IReadOnlyList<RuleSimulationStep> Steps);
public record RuleSimulationStep(string RuleName, string RuleType, bool Fired, string Outcome, string? Detail);


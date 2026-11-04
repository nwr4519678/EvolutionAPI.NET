using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EvolutionApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Tenant",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LogoUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    Timezone = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "UTC"),
                    Language = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "en"),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PlanId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ContactEmail = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ContactPhone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenant", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UsageDailyAggregate",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    NumberId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    MessagesIn = table.Column<int>(type: "int", nullable: false),
                    MessagesOut = table.Column<int>(type: "int", nullable: false),
                    AiCalls = table.Column<int>(type: "int", nullable: false),
                    TokensIn = table.Column<long>(type: "bigint", nullable: false),
                    TokensOut = table.Column<long>(type: "bigint", nullable: false),
                    TotalCost = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    AvgLatencyMs = table.Column<int>(type: "int", nullable: false),
                    SuccessCount = table.Column<int>(type: "int", nullable: false),
                    FailureCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsageDailyAggregate", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AiProvider",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ProviderType = table.Column<int>(type: "int", nullable: false),
                    BaseUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    ApiKeyEncrypted = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DefaultModel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Temperature = table.Column<double>(type: "float", nullable: false),
                    MaxTokens = table.Column<int>(type: "int", nullable: false),
                    TimeoutSeconds = table.Column<int>(type: "int", nullable: false),
                    PricePerKInputTokens = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    PricePerKOutputTokens = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiProvider", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiProvider_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenant",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AuditLog",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    UserEmail = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    Before = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    After = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLog_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenant",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Notification",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ActionUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    ReadAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notification", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notification_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlatformApiKey",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    KeyHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    KeyPrefix = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Scopes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AllowedIps = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastUsedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformApiKey", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlatformApiKey_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SystemPrompt",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    NumberId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CurrentVersionId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemPrompt", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SystemPrompt_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WebhookEndpoint",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Url = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    Events = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SecretEncrypted = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    MaxRetries = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhookEndpoint", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WebhookEndpoint_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WhatsAppNumber",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ConnectionMethod = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    LinkedInstanceId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    AiProviderId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    SystemPromptId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    BotEnabled = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WhatsAppNumber", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WhatsAppNumber_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SystemPromptVersion",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    PromptId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AuthorId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    IsDraft = table.Column<bool>(type: "bit", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemPromptVersion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SystemPromptVersion_SystemPrompt_PromptId",
                        column: x => x.PromptId,
                        principalTable: "SystemPrompt",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlatformWebhookDelivery",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    EndpointId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HttpStatusCode = table.Column<int>(type: "int", nullable: true),
                    ResponseBody = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    NextRetryAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformWebhookDelivery", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlatformWebhookDelivery_WebhookEndpoint_EndpointId",
                        column: x => x.EndpointId,
                        principalTable: "WebhookEndpoint",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BotRule",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    NumberId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    RuleType = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    ConfigJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    WhatsAppNumberId = table.Column<string>(type: "nvarchar(36)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BotRule", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BotRule_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BotRule_WhatsAppNumber_WhatsAppNumberId",
                        column: x => x.WhatsAppNumberId,
                        principalTable: "WhatsAppNumber",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Conversation",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    NumberId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    ContactPhone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ContactName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    BotPaused = table.Column<bool>(type: "bit", nullable: false),
                    AssignedAgentId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    LastMessagePreview = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LastMessageAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    MessageCount = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conversation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Conversation_WhatsAppNumber_NumberId",
                        column: x => x.NumberId,
                        principalTable: "WhatsAppNumber",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UsageRecord",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    NumberId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    ConversationId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    MessageId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    Direction = table.Column<int>(type: "int", nullable: false),
                    EventType = table.Column<int>(type: "int", nullable: false),
                    AiProviderId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    AiModel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TokensIn = table.Column<int>(type: "int", nullable: false),
                    TokensOut = table.Column<int>(type: "int", nullable: false),
                    Cost = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    LatencyMs = table.Column<int>(type: "int", nullable: false),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                    ErrorCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    WhatsAppNumberId = table.Column<string>(type: "nvarchar(36)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsageRecord", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsageRecord_Tenant_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UsageRecord_WhatsAppNumber_WhatsAppNumberId",
                        column: x => x.WhatsAppNumberId,
                        principalTable: "WhatsAppNumber",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ConversationMessage",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    ConversationId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    NumberId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    Direction = table.Column<int>(type: "int", nullable: false),
                    Sender = table.Column<int>(type: "int", nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MediaType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MediaUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    ProviderMessageId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsFromBot = table.Column<bool>(type: "bit", nullable: false),
                    AiProviderId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    AiModel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TokensIn = table.Column<int>(type: "int", nullable: true),
                    TokensOut = table.Column<int>(type: "int", nullable: true),
                    AiCost = table.Column<decimal>(type: "decimal(18,8)", nullable: true),
                    AiLatencyMs = table.Column<int>(type: "int", nullable: true),
                    SentAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConversationMessage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConversationMessage_Conversation_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "Conversation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiProvider_TenantId",
                table: "AiProvider",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_TenantId_OccurredAt",
                table: "AuditLog",
                columns: new[] { "TenantId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BotRule_TenantId_Priority",
                table: "BotRule",
                columns: new[] { "TenantId", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_BotRule_WhatsAppNumberId",
                table: "BotRule",
                column: "WhatsAppNumberId");

            migrationBuilder.CreateIndex(
                name: "IX_Conversation_NumberId",
                table: "Conversation",
                column: "NumberId");

            migrationBuilder.CreateIndex(
                name: "IX_Conversation_TenantId_ContactPhone",
                table: "Conversation",
                columns: new[] { "TenantId", "ContactPhone" });

            migrationBuilder.CreateIndex(
                name: "IX_Conversation_TenantId_NumberId",
                table: "Conversation",
                columns: new[] { "TenantId", "NumberId" });

            migrationBuilder.CreateIndex(
                name: "IX_ConversationMessage_ConversationId",
                table: "ConversationMessage",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationMessage_TenantId",
                table: "ConversationMessage",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Notification_TenantId_UserId_IsRead",
                table: "Notification",
                columns: new[] { "TenantId", "UserId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_PlatformApiKey_KeyHash",
                table: "PlatformApiKey",
                column: "KeyHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlatformApiKey_TenantId",
                table: "PlatformApiKey",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformWebhookDelivery_EndpointId",
                table: "PlatformWebhookDelivery",
                column: "EndpointId");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformWebhookDelivery_TenantId",
                table: "PlatformWebhookDelivery",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemPrompt_TenantId",
                table: "SystemPrompt",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemPromptVersion_PromptId_VersionNumber",
                table: "SystemPromptVersion",
                columns: new[] { "PromptId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenant_Slug",
                table: "Tenant",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UsageDailyAggregate_TenantId_NumberId_Date",
                table: "UsageDailyAggregate",
                columns: new[] { "TenantId", "NumberId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UsageRecord_TenantId_NumberId_OccurredAt",
                table: "UsageRecord",
                columns: new[] { "TenantId", "NumberId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UsageRecord_TenantId_OccurredAt",
                table: "UsageRecord",
                columns: new[] { "TenantId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UsageRecord_WhatsAppNumberId",
                table: "UsageRecord",
                column: "WhatsAppNumberId");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookEndpoint_TenantId",
                table: "WebhookEndpoint",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_WhatsAppNumber_TenantId",
                table: "WhatsAppNumber",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiProvider");

            migrationBuilder.DropTable(
                name: "AuditLog");

            migrationBuilder.DropTable(
                name: "BotRule");

            migrationBuilder.DropTable(
                name: "ConversationMessage");

            migrationBuilder.DropTable(
                name: "Notification");

            migrationBuilder.DropTable(
                name: "PlatformApiKey");

            migrationBuilder.DropTable(
                name: "PlatformWebhookDelivery");

            migrationBuilder.DropTable(
                name: "SystemPromptVersion");

            migrationBuilder.DropTable(
                name: "UsageDailyAggregate");

            migrationBuilder.DropTable(
                name: "UsageRecord");

            migrationBuilder.DropTable(
                name: "Conversation");

            migrationBuilder.DropTable(
                name: "WebhookEndpoint");

            migrationBuilder.DropTable(
                name: "SystemPrompt");

            migrationBuilder.DropTable(
                name: "WhatsAppNumber");

            migrationBuilder.DropTable(
                name: "Tenant");
        }
    }
}

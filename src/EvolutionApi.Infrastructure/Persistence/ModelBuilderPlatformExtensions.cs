using EvolutionApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EvolutionApi.Infrastructure.Persistence;

public static class ModelBuilderPlatformExtensions
{
    public static void ApplyPlatformConfigurations(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tenant>(b =>
        {
            b.ToTable("Tenant");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasMaxLength(36).IsRequired();
            b.Property(x => x.Name).HasMaxLength(255).IsRequired();
            b.Property(x => x.Slug).HasMaxLength(100).IsRequired();
            b.Property(x => x.LogoUrl).HasMaxLength(2048);
            b.Property(x => x.Timezone).HasMaxLength(100).HasDefaultValue("UTC");
            b.Property(x => x.Language).HasMaxLength(10).HasDefaultValue("en");
            b.Property(x => x.ContactEmail).HasMaxLength(255);
            b.Property(x => x.ContactPhone).HasMaxLength(50);
            b.Property(x => x.PlanId).HasMaxLength(50);
            b.HasIndex(x => x.Slug).IsUnique();
            b.HasQueryFilter(x => !x.IsDeleted);
        });

        modelBuilder.Entity<WhatsAppNumber>(b =>
        {
            b.ToTable("WhatsAppNumber");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasMaxLength(36).IsRequired();
            b.Property(x => x.TenantId).HasMaxLength(36).IsRequired();
            b.Property(x => x.DisplayName).HasMaxLength(255).IsRequired();
            b.Property(x => x.PhoneNumber).HasMaxLength(50);
            b.Property(x => x.LinkedInstanceId).HasMaxLength(36);
            b.Property(x => x.AiProviderId).HasMaxLength(36);
            b.Property(x => x.SystemPromptId).HasMaxLength(36);
            b.HasIndex(x => x.TenantId);
            b.HasQueryFilter(x => !x.IsDeleted);
            b.HasOne(x => x.Tenant)
                .WithMany(t => t.WhatsAppNumbers)
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Conversation>(b =>
        {
            b.ToTable("Conversation");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasMaxLength(36).IsRequired();
            b.Property(x => x.TenantId).HasMaxLength(36).IsRequired();
            b.Property(x => x.NumberId).HasMaxLength(36).IsRequired();
            b.Property(x => x.ContactPhone).HasMaxLength(50).IsRequired();
            b.Property(x => x.ContactName).HasMaxLength(255);
            b.Property(x => x.AssignedAgentId).HasMaxLength(36);
            b.Property(x => x.LastMessagePreview).HasMaxLength(500);
            b.HasIndex(x => new { x.TenantId, x.NumberId });
            b.HasIndex(x => new { x.TenantId, x.ContactPhone });
            b.HasQueryFilter(x => !x.IsDeleted);
            b.HasOne(x => x.WhatsAppNumber)
                .WithMany(n => n.Conversations)
                .HasForeignKey(x => x.NumberId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ConversationMessage>(b =>
        {
            b.ToTable("ConversationMessage");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasMaxLength(36).IsRequired();
            b.Property(x => x.TenantId).HasMaxLength(36).IsRequired();
            b.Property(x => x.ConversationId).HasMaxLength(36).IsRequired();
            b.Property(x => x.NumberId).HasMaxLength(36).IsRequired();
            b.Property(x => x.Body).HasColumnType("nvarchar(max)");
            b.Property(x => x.MediaType).HasMaxLength(50);
            b.Property(x => x.MediaUrl).HasMaxLength(2048);
            b.Property(x => x.ProviderMessageId).HasMaxLength(255);
            b.Property(x => x.AiProviderId).HasMaxLength(36);
            b.Property(x => x.AiModel).HasMaxLength(100);
            b.Property(x => x.AiCost).HasColumnType("decimal(18,8)");
            b.HasIndex(x => x.ConversationId);
            b.HasIndex(x => x.TenantId);
            b.HasOne(x => x.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(x => x.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AiProvider>(b =>
        {
            b.ToTable("AiProvider");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasMaxLength(36).IsRequired();
            b.Property(x => x.TenantId).HasMaxLength(36);
            b.Property(x => x.Name).HasMaxLength(255).IsRequired();
            b.Property(x => x.BaseUrl).HasMaxLength(2048);
            b.Property(x => x.ApiKeyEncrypted).HasColumnType("nvarchar(max)");
            b.Property(x => x.DefaultModel).HasMaxLength(100).IsRequired();
            b.Property(x => x.PricePerKInputTokens).HasColumnType("decimal(18,8)");
            b.Property(x => x.PricePerKOutputTokens).HasColumnType("decimal(18,8)");
            b.HasIndex(x => x.TenantId);
            b.HasQueryFilter(x => !x.IsDeleted);
        });

        modelBuilder.Entity<SystemPrompt>(b =>
        {
            b.ToTable("SystemPrompt");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasMaxLength(36).IsRequired();
            b.Property(x => x.TenantId).HasMaxLength(36).IsRequired();
            b.Property(x => x.NumberId).HasMaxLength(36);
            b.Property(x => x.Name).HasMaxLength(255).IsRequired();
            b.Property(x => x.Description).HasMaxLength(1000);
            b.Property(x => x.CurrentVersionId).HasMaxLength(36);
            b.Property(x => x.CreatedBy).HasMaxLength(36);
            b.HasIndex(x => x.TenantId);
            b.HasQueryFilter(x => !x.IsDeleted);
        });

        modelBuilder.Entity<SystemPromptVersion>(b =>
        {
            b.ToTable("SystemPromptVersion");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasMaxLength(36).IsRequired();
            b.Property(x => x.PromptId).HasMaxLength(36).IsRequired();
            b.Property(x => x.TenantId).HasMaxLength(36).IsRequired();
            b.Property(x => x.Content).HasColumnType("nvarchar(max)").IsRequired();
            b.Property(x => x.Note).HasMaxLength(500);
            b.Property(x => x.AuthorId).HasMaxLength(36);
            b.HasIndex(x => new { x.PromptId, x.VersionNumber }).IsUnique();
            b.HasOne(x => x.Prompt)
                .WithMany(p => p.Versions)
                .HasForeignKey(x => x.PromptId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BotRule>(b =>
        {
            b.ToTable("BotRule");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasMaxLength(36).IsRequired();
            b.Property(x => x.TenantId).HasMaxLength(36).IsRequired();
            b.Property(x => x.NumberId).HasMaxLength(36);
            b.Property(x => x.Name).HasMaxLength(255).IsRequired();
            b.Property(x => x.ConfigJson).HasColumnType("nvarchar(max)");
            b.HasIndex(x => new { x.TenantId, x.Priority });
            b.HasQueryFilter(x => !x.IsDeleted);
        });

        modelBuilder.Entity<UsageRecord>(b =>
        {
            b.ToTable("UsageRecord");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasMaxLength(36).IsRequired();
            b.Property(x => x.TenantId).HasMaxLength(36).IsRequired();
            b.Property(x => x.NumberId).HasMaxLength(36).IsRequired();
            b.Property(x => x.ConversationId).HasMaxLength(36);
            b.Property(x => x.MessageId).HasMaxLength(36);
            b.Property(x => x.AiProviderId).HasMaxLength(36);
            b.Property(x => x.AiModel).HasMaxLength(100);
            b.Property(x => x.Cost).HasColumnType("decimal(18,8)");
            b.Property(x => x.ErrorCode).HasMaxLength(100);
            b.HasIndex(x => new { x.TenantId, x.OccurredAt });
            b.HasIndex(x => new { x.TenantId, x.NumberId, x.OccurredAt });
        });

        modelBuilder.Entity<UsageDailyAggregate>(b =>
        {
            b.ToTable("UsageDailyAggregate");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasMaxLength(36).IsRequired();
            b.Property(x => x.TenantId).HasMaxLength(36).IsRequired();
            b.Property(x => x.NumberId).HasMaxLength(36).IsRequired();
            b.Property(x => x.TotalCost).HasColumnType("decimal(18,8)");
            b.HasIndex(x => new { x.TenantId, x.NumberId, x.Date }).IsUnique();
        });

        modelBuilder.Entity<PlatformApiKey>(b =>
        {
            b.ToTable("PlatformApiKey");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasMaxLength(36).IsRequired();
            b.Property(x => x.TenantId).HasMaxLength(36).IsRequired();
            b.Property(x => x.Name).HasMaxLength(255).IsRequired();
            b.Property(x => x.KeyHash).HasMaxLength(64).IsRequired();
            b.Property(x => x.KeyPrefix).HasMaxLength(20);
            b.Property(x => x.CreatedBy).HasMaxLength(36);
            b.Property(x => x.Scopes).HasConversion(
                v => string.Join(',', v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries));
            b.Property(x => x.AllowedIps).HasConversion(
                v => v == null ? null : string.Join(',', v),
                v => v == null ? null : v.Split(',', StringSplitOptions.RemoveEmptyEntries));
            b.HasIndex(x => x.KeyHash).IsUnique();
            b.HasIndex(x => x.TenantId);
            b.HasQueryFilter(x => !x.IsDeleted);
        });

        modelBuilder.Entity<WebhookEndpoint>(b =>
        {
            b.ToTable("WebhookEndpoint");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasMaxLength(36).IsRequired();
            b.Property(x => x.TenantId).HasMaxLength(36).IsRequired();
            b.Property(x => x.Name).HasMaxLength(255).IsRequired();
            b.Property(x => x.Url).HasMaxLength(2048).IsRequired();
            b.Property(x => x.SecretEncrypted).HasColumnType("nvarchar(max)");
            b.Property(x => x.Events).HasConversion(
                v => string.Join(',', v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries));
            b.HasIndex(x => x.TenantId);
            b.HasQueryFilter(x => !x.IsDeleted);
        });

        modelBuilder.Entity<PlatformWebhookDelivery>(b =>
        {
            b.ToTable("PlatformWebhookDelivery");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasMaxLength(36).IsRequired();
            b.Property(x => x.EndpointId).HasMaxLength(36).IsRequired();
            b.Property(x => x.TenantId).HasMaxLength(36).IsRequired();
            b.Property(x => x.EventType).HasMaxLength(100).IsRequired();
            b.Property(x => x.Payload).HasColumnType("nvarchar(max)").IsRequired();
            b.Property(x => x.ResponseBody).HasColumnType("nvarchar(max)");
            b.Property(x => x.ErrorMessage).HasMaxLength(1000);
            b.HasIndex(x => x.EndpointId);
            b.HasIndex(x => x.TenantId);
            b.HasOne(x => x.Endpoint)
                .WithMany(e => e.Deliveries)
                .HasForeignKey(x => x.EndpointId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AuditLog>(b =>
        {
            b.ToTable("AuditLog");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasMaxLength(36).IsRequired();
            b.Property(x => x.TenantId).HasMaxLength(36);
            b.Property(x => x.UserId).HasMaxLength(36);
            b.Property(x => x.UserEmail).HasMaxLength(255);
            b.Property(x => x.Action).HasMaxLength(100).IsRequired();
            b.Property(x => x.EntityType).HasMaxLength(100).IsRequired();
            b.Property(x => x.EntityId).HasMaxLength(36);
            b.Property(x => x.Before).HasColumnType("nvarchar(max)");
            b.Property(x => x.After).HasColumnType("nvarchar(max)");
            b.Property(x => x.IpAddress).HasMaxLength(50);
            b.Property(x => x.UserAgent).HasMaxLength(500);
            b.HasIndex(x => new { x.TenantId, x.OccurredAt });
        });

        modelBuilder.Entity<Notification>(b =>
        {
            b.ToTable("Notification");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasMaxLength(36).IsRequired();
            b.Property(x => x.TenantId).HasMaxLength(36).IsRequired();
            b.Property(x => x.UserId).HasMaxLength(36);
            b.Property(x => x.Title).HasMaxLength(255).IsRequired();
            b.Property(x => x.Body).HasMaxLength(1000).IsRequired();
            b.Property(x => x.ActionUrl).HasMaxLength(2048);
            b.HasIndex(x => new { x.TenantId, x.UserId, x.IsRead });
        });
    }
}


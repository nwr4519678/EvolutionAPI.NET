using EvolutionApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EvolutionApi.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for the Evolution API.
/// All entity configurations are explicitly mapped in OnModelCreating via strongly-typed Fluent API — zero raw SQL.
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Instance> Instances => Set<Instance>();
    public DbSet<WebhookConfig> WebhookConfigs => Set<WebhookConfig>();
    public DbSet<InstanceProxy> InstanceProxies => Set<InstanceProxy>();
    public DbSet<InstanceSettings> InstanceSettings => Set<InstanceSettings>();
    public DbSet<SessionAuth> SessionAuths => Set<SessionAuth>();
    public DbSet<WebhookDelivery> WebhookDeliveries => Set<WebhookDelivery>();
    public DbSet<OutboundMessage> OutboundMessages => Set<OutboundMessage>();
    public DbSet<Chat> Chats => Set<Chat>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<TypebotSetting> TypebotSettings => Set<TypebotSetting>();
    public DbSet<TypebotSession> TypebotSessions => Set<TypebotSession>();
    public DbSet<ChatwootSetting> ChatwootSettings => Set<ChatwootSetting>();
    public DbSet<OpenAiSetting> OpenAiSettings => Set<OpenAiSetting>();
    public DbSet<OpenAiSession> OpenAiSessions => Set<OpenAiSession>();
    public DbSet<RabbitMqSetting> RabbitMqSettings => Set<RabbitMqSetting>();
    public DbSet<SqsSetting> SqsSettings => Set<SqsSetting>();
    public DbSet<NatsSetting> NatsSettings => Set<NatsSetting>();
    public DbSet<SignalKey> SignalKeys => Set<SignalKey>();
    public DbSet<ScheduledMessageJob> ScheduledMessages => Set<ScheduledMessageJob>();
    public DbSet<ChatFlowSession> ChatFlowSessions => Set<ChatFlowSession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Instance ───────────────────────────────────────────────────────────────
        modelBuilder.Entity<Instance>(b =>
        {
            b.ToTable("Instance");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasMaxLength(36).IsRequired();
            b.Property(x => x.Name).HasMaxLength(255).IsRequired();
            b.Property(x => x.ApiKeyHash).HasMaxLength(64).IsRequired();
            b.Property(x => x.Number).HasMaxLength(32);
            b.Property(x => x.Integration).HasMaxLength(100).IsRequired();
            b.Property(x => x.BusinessId).HasMaxLength(100);
            b.Property(x => x.ConnectionStatus).HasMaxLength(32).IsRequired();
            b.HasIndex(x => x.Name).IsUnique();

            b.HasOne(x => x.Webhook)
                .WithOne(w => w.Instance)
                .HasForeignKey<WebhookConfig>(x => x.InstanceId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(x => x.Proxy)
                .WithOne(p => p.Instance)
                .HasForeignKey<InstanceProxy>(x => x.InstanceId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(x => x.Settings)
                .WithOne(s => s.Instance)
                .HasForeignKey<InstanceSettings>(x => x.InstanceId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasMany(x => x.AuthRecords)
                .WithOne(a => a.Instance)
                .HasForeignKey(x => x.InstanceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── WebhookConfig ──────────────────────────────────────────────────────────
        modelBuilder.Entity<WebhookConfig>(b =>
        {
            b.ToTable("Webhook");
            b.HasKey(x => x.Id);
            b.Property(x => x.InstanceId).HasMaxLength(36).IsRequired();
            b.Property(x => x.Url).HasMaxLength(2048);
            b.Property(x => x.EventsJson).HasColumnType("nvarchar(max)");
            b.Property(x => x.HeadersJson).HasColumnType("nvarchar(max)");
            b.HasIndex(x => x.InstanceId).IsUnique();
        });

        // ── InstanceProxy ──────────────────────────────────────────────────────────
        modelBuilder.Entity<InstanceProxy>(b =>
        {
            b.ToTable("Proxy");
            b.HasKey(x => x.Id);
            b.Property(x => x.InstanceId).HasMaxLength(36).IsRequired();
            b.Property(x => x.Host).HasMaxLength(100).IsRequired();
            b.Property(x => x.Port).HasMaxLength(10).IsRequired();
            b.Property(x => x.Protocol).HasMaxLength(10).IsRequired();
            b.Property(x => x.Username).HasMaxLength(100).IsRequired();
            b.Property(x => x.PasswordEncrypted).HasMaxLength(1024).IsRequired();
            b.HasIndex(x => x.InstanceId).IsUnique();
        });

        // ── InstanceSettings ───────────────────────────────────────────────────────
        modelBuilder.Entity<InstanceSettings>(b =>
        {
            b.ToTable("Setting");
            b.HasKey(x => x.Id);
            b.Property(x => x.InstanceId).HasMaxLength(36).IsRequired();
            b.Property(x => x.MsgCall).HasMaxLength(100);
            b.Property(x => x.WavoipToken).HasMaxLength(100);
            b.HasIndex(x => x.InstanceId).IsUnique();
        });

        // ── SessionAuth ────────────────────────────────────────────────────────────
        modelBuilder.Entity<SessionAuth>(b =>
        {
            b.ToTable("SessionAuth");
            b.HasKey(x => x.Id);
            b.Property(x => x.InstanceId).HasMaxLength(36).IsRequired();
            b.Property(x => x.SessionKey).HasMaxLength(255).IsRequired();
            b.Property(x => x.EncryptedJson).HasColumnType("nvarchar(max)").IsRequired();
            b.HasIndex(x => new { x.InstanceId, x.SessionKey }).IsUnique();
        });

        // ── WebhookDelivery ────────────────────────────────────────────────────────
        modelBuilder.Entity<WebhookDelivery>(b =>
        {
            b.ToTable("WebhookDelivery");
            b.HasKey(x => x.Id);
            b.Property(x => x.InstanceName).HasMaxLength(255).IsRequired();
            b.Property(x => x.EventName).HasMaxLength(100).IsRequired();
            b.Property(x => x.Destination).HasMaxLength(2048).IsRequired();
            b.Property(x => x.HeadersJson).HasColumnType("nvarchar(max)");
            b.Property(x => x.PayloadJson).HasColumnType("nvarchar(max)").IsRequired();
            b.Property(x => x.Status).HasMaxLength(32).IsRequired();
            b.HasIndex(x => new { x.Status, x.NextAttemptAt });
        });

        // ── OutboundMessage ────────────────────────────────────────────────────────
        modelBuilder.Entity<OutboundMessage>(b =>
        {
            b.ToTable("OutboundMessage");
            b.HasKey(x => x.Id);
            b.Property(x => x.InstanceId).HasMaxLength(36).IsRequired();
            b.Property(x => x.Operation).HasMaxLength(64).IsRequired();
            b.Property(x => x.PayloadJson).HasColumnType("nvarchar(max)").IsRequired();
            b.Property(x => x.Status).HasMaxLength(32).IsRequired();
            b.HasIndex(x => new { x.Status, x.NextAttemptAt });
        });

        // ── Chat ───────────────────────────────────────────────────────────────────
        modelBuilder.Entity<Chat>(b =>
        {
            b.ToTable("Chat");
            b.HasKey(x => new { x.Id, x.InstanceId });
            b.Property(x => x.Id).HasMaxLength(128).IsRequired();
            b.Property(x => x.InstanceId).HasMaxLength(36).IsRequired();
            b.Property(x => x.Name).HasMaxLength(255);
            b.Property(x => x.LastMessageJson).HasColumnType("nvarchar(max)");
        });

        // ── Message ────────────────────────────────────────────────────────────────
        modelBuilder.Entity<Message>(b =>
        {
            b.ToTable("Message");
            b.HasKey(x => new { x.Id, x.InstanceId });
            b.Property(x => x.Id).HasMaxLength(128).IsRequired();
            b.Property(x => x.InstanceId).HasMaxLength(36).IsRequired();
            b.Property(x => x.RemoteJid).HasMaxLength(128).IsRequired();
            b.Property(x => x.Participant).HasMaxLength(128);
            b.Property(x => x.PushName).HasMaxLength(255);
            b.Property(x => x.MessageJson).HasColumnType("nvarchar(max)").IsRequired();
            b.Property(x => x.MessageType).HasMaxLength(64);
            b.Property(x => x.Status).HasMaxLength(32).IsRequired();
            b.HasIndex(x => new { x.InstanceId, x.RemoteJid, x.Timestamp });
        });

        // ── Contact ────────────────────────────────────────────────────────────────
        modelBuilder.Entity<Contact>(b =>
        {
            b.ToTable("Contact");
            b.HasKey(x => new { x.Id, x.InstanceId });
            b.Property(x => x.Id).HasMaxLength(128).IsRequired();
            b.Property(x => x.InstanceId).HasMaxLength(36).IsRequired();
            b.Property(x => x.PushName).HasMaxLength(255);
            b.Property(x => x.VerifiedName).HasMaxLength(255);
            b.Property(x => x.ProfilePicUrl).HasMaxLength(2048);
        });

        // ── Typebot ────────────────────────────────────────────────────────────────
        modelBuilder.Entity<TypebotSetting>(b =>
        {
            b.ToTable("TypebotSetting");
            b.HasKey(x => x.Id);
            b.Property(x => x.InstanceId).HasMaxLength(36).IsRequired();
            b.HasIndex(x => x.InstanceId).IsUnique();
        });

        modelBuilder.Entity<TypebotSession>(b =>
        {
            b.ToTable("TypebotSession");
            b.HasKey(x => x.Id);
            b.Property(x => x.InstanceId).HasMaxLength(36).IsRequired();
            b.Property(x => x.RemoteJid).HasMaxLength(128).IsRequired();
            b.HasIndex(x => new { x.InstanceId, x.RemoteJid });
        });

        // ── Chatwoot ───────────────────────────────────────────────────────────────
        modelBuilder.Entity<ChatwootSetting>(b =>
        {
            b.ToTable("ChatwootSetting");
            b.HasKey(x => x.Id);
            b.Property(x => x.InstanceId).HasMaxLength(36).IsRequired();
            b.HasIndex(x => x.InstanceId).IsUnique();
        });

        // ── OpenAI ─────────────────────────────────────────────────────────────────
        modelBuilder.Entity<OpenAiSetting>(b =>
        {
            b.ToTable("OpenAiSetting");
            b.HasKey(x => x.Id);
            b.Property(x => x.InstanceId).HasMaxLength(36).IsRequired();
            b.HasIndex(x => x.InstanceId).IsUnique();
        });

        modelBuilder.Entity<OpenAiSession>(b =>
        {
            b.ToTable("OpenAiSession");
            b.HasKey(x => x.Id);
            b.Property(x => x.InstanceId).HasMaxLength(36).IsRequired();
            b.Property(x => x.RemoteJid).HasMaxLength(128).IsRequired();
            b.HasIndex(x => new { x.InstanceId, x.RemoteJid });
        });

        // ── Event Transports (RabbitMQ, SQS, NATS) ─────────────────────────────────
        modelBuilder.Entity<RabbitMqSetting>(b =>
        {
            b.ToTable("RabbitMqSetting");
            b.HasKey(x => x.Id);
            b.Property(x => x.InstanceId).HasMaxLength(36).IsRequired();
            b.HasIndex(x => x.InstanceId).IsUnique();
        });

        modelBuilder.Entity<SqsSetting>(b =>
        {
            b.ToTable("SqsSetting");
            b.HasKey(x => x.Id);
            b.Property(x => x.InstanceId).HasMaxLength(36).IsRequired();
            b.HasIndex(x => x.InstanceId).IsUnique();
        });

        modelBuilder.Entity<NatsSetting>(b =>
        {
            b.ToTable("NatsSetting");
            b.HasKey(x => x.Id);
            b.Property(x => x.InstanceId).HasMaxLength(36).IsRequired();
            b.HasIndex(x => x.InstanceId).IsUnique();
        });

        // ── SignalKey (Baileys.NET ISignalKeyStore persistence) ───────────────────
        modelBuilder.Entity<SignalKey>(b =>
        {
            b.ToTable("SignalKey");
            b.HasKey(x => x.Id);
            b.Property(x => x.InstanceId).HasMaxLength(36).IsRequired();
            b.Property(x => x.Category).HasMaxLength(64).IsRequired();
            b.Property(x => x.KeyId).HasMaxLength(256).IsRequired();
            b.Property(x => x.KeyData).IsRequired();
            // Composite unique index — same key cannot be stored twice per instance
            b.HasIndex(x => new { x.InstanceId, x.Category, x.KeyId }).IsUnique();
            b.HasOne(x => x.Instance)
                .WithMany()
                .HasForeignKey(x => x.InstanceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── ScheduledMessageJob (Hangfire scheduled messages) ────────────────────
        modelBuilder.Entity<ScheduledMessageJob>(b =>
        {
            b.ToTable("ScheduledMessageJob");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasMaxLength(36).IsRequired();
            b.Property(x => x.InstanceId).HasMaxLength(36).IsRequired();
            b.Property(x => x.TenantId).HasMaxLength(128);
            b.Property(x => x.Recipient).HasMaxLength(128).IsRequired();
            b.Property(x => x.Operation).HasMaxLength(64).IsRequired();
            b.Property(x => x.PayloadJson).HasColumnType("nvarchar(max)").IsRequired();
            b.Property(x => x.Status).HasMaxLength(32).IsRequired();
            b.Property(x => x.HangfireJobId).HasMaxLength(128);
            b.HasIndex(x => new { x.Status, x.ScheduledAtUtc });
            b.HasOne(x => x.Instance)
                .WithMany(i => i.ScheduledMessages)
                .HasForeignKey(x => x.InstanceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── ChatFlowSession ───────────────────────────────────────────────────────
        modelBuilder.Entity<ChatFlowSession>(b =>
        {
            b.ToTable("ChatFlowSession");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasMaxLength(36).IsRequired();
            b.Property(x => x.InstanceId).HasMaxLength(36).IsRequired();
            b.Property(x => x.TenantId).HasMaxLength(128);
            b.Property(x => x.RemoteJid).HasMaxLength(128).IsRequired();
            b.Property(x => x.CurrentNodeId).HasMaxLength(128).IsRequired();
            b.Property(x => x.Status).HasMaxLength(32).IsRequired();
            b.Property(x => x.VariablesJson).HasColumnType("nvarchar(max)").IsRequired();
            b.HasIndex(x => new { x.InstanceId, x.RemoteJid });
            b.HasOne(x => x.Instance)
                .WithMany(i => i.ChatFlowSessions)
                .HasForeignKey(x => x.InstanceId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

using EvolutionApi.Domain.Interfaces;
using EvolutionApi.Application.Common.Monitoring;
using EvolutionApi.Infrastructure.Persistence;
using EvolutionApi.Infrastructure.Persistence.Repositories;
using EvolutionApi.Infrastructure.RateLimiting;
using EvolutionApi.Infrastructure.Hubs;
using EvolutionApi.Infrastructure.Monitoring;
using EvolutionApi.Infrastructure.Scheduling;
using EvolutionApi.Infrastructure.Security;
using EvolutionApi.Infrastructure.Services;
using EvolutionApi.Infrastructure.Services.EventTransport;
using EvolutionApi.Infrastructure.Services.Notifications;
using EvolutionApi.Infrastructure.WhatsApp.Engine;
using EvolutionApi.Infrastructure.WhatsApp.Services;
using EvolutionApi.Infrastructure.WhatsApp.State;
using EvolutionApi.Infrastructure.WhatsApp.Throttling;
using FluentValidation;
using Hangfire;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.SignalR;

namespace EvolutionApi.Infrastructure;

/// <summary>
/// Infrastructure layer DI registration.
/// Call services.AddInfrastructure(configuration) from Program.cs.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── EF Core SQL Server ────────────────────────────────────────────────────
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? configuration["DATABASE_URL"]
            ?? configuration["Evolution:Database"]
            ?? throw new InvalidOperationException
            ("Connection string 'DefaultConnection' was not found in configuration.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                sql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null);
            }));
        services.AddSingleton<ProcessCpuSampler>();
        services.AddScoped<IDashboardUsageProvider, DashboardUsageProvider>();

        // ── Repositories & Unit of Work ────────────────────────────────────────────
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IInstanceRepository, InstanceRepository>();
        services.AddScoped<IWebhookRepository, WebhookRepository>();
        services.AddScoped<IChatRepository, ChatRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<ISettingsRepository, SettingsRepository>();
        services.AddScoped<IIntegrationRepository, IntegrationRepository>();
        services.AddScoped<IScheduledMessageRepository, ScheduledMessageRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // ── Data Protection (Tenant Session Encryption) ───────────────────────────
        var dataProtection = services.AddDataProtection()
            .SetApplicationName("EvolutionApi.Gateway");
        var keyPath = configuration["DataProtection:KeyPath"];
        if (!string.IsNullOrWhiteSpace(keyPath))
            dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keyPath));
        services.AddSingleton<ISecretProtector, DataProtectionSecretProtector>();

        // ── Finite State Machine (Stateless) ──────────────────────────────────────
        services.AddSingleton<IInstanceLifecycleFsmFactory, InstanceLifecycleFsmFactory>();

        // ── System.Threading.Channels (Per-Tenant Throttler Queue) ─────────────────
        services.AddSingleton<PerTenantOutboundChannelQueue>();
        services.AddSingleton<IThrottledMessageQueue>(sp => sp.GetRequiredService<PerTenantOutboundChannelQueue>());

        // ── Hangfire (Scheduled Out-of-Process Background Jobs) ────────────────────
        services.AddHangfire(cfg =>
        {
            cfg.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
               .UseSimpleAssemblyNameTypeSerializer()
               .UseRecommendedSerializerSettings()
               .UseSqlServerStorage(connectionString);
        });
        services.AddHangfireServer(options =>
        {
            // Limit to at most 2 worker threads to keep memory footprint minimal on shared hosts like MonsterASP
            options.WorkerCount = Math.Min(2, Math.Max(1, Environment.ProcessorCount));
        });
        services.AddScoped<IScheduledJobService, HangfireScheduledJobService>();
        services.AddScoped<ScheduledMessageJobExecutor>();
        services.AddScoped<WebhookRetryJobExecutor>();

        // ── Hybrid Caching ────────────────────────────────────────────────────────
        services.AddMemoryCache();
        services.AddHybridCache(options =>
        {
            // Prevent large payloads from bloating memory on low-RAM hosts
            options.MaximumPayloadBytes = 1024 * 1024; // 1 MB cap per cached entry
            options.MaximumKeyLength = 256;
            options.DefaultEntryOptions = new Microsoft.Extensions.Caching.Hybrid.HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(3),
                LocalCacheExpiration = TimeSpan.FromMinutes(3)
            };
        });
        services.AddSingleton<ICacheService, HybridCacheService>();

        // ── Security & Authentication (FluentValidation) ──────────────────────────
        services.AddScoped<IValidator<ApiKeyValidationRequest>, ApiKeyValidationRequestValidator>();
        services.AddScoped<IApiKeyValidator, ApiKeyValidator>();
        services.AddSingleton<HangfireDashboardAuthorizationFilter>();
        services.AddSingleton<OutboundUrlValidator>();

        // ── HTTP Clients ──────────────────────────────────────────────────────────
        services.AddHttpClient("webhooks", c => c.Timeout = TimeSpan.FromSeconds(30));
        services.AddHttpClient("media", c => c.Timeout = TimeSpan.FromSeconds(30));

        // ── WhatsApp Engine & Cryptographic Services ─────────────────────────────
        services.AddSingleton<SessionKeyStore>();
        services.AddSingleton<WaMediaUploader>();
        services.AddSingleton<WebSocketTransport>();
        services.AddSingleton<WaInboundMessageStore>();
        services.AddSingleton<WaRawFrameInterceptor>();
        services.AddSingleton<WebhookDeliveryQueue>();
        services.AddSingleton<OutboundMessageQueue>();
        services.AddScoped<MessageStoreService>();
        services.AddScoped<MessageService>();
        services.AddScoped<WebhookService>();

        // ── Real-time Streaming with SignalR ─────────────────────────────────────
        services.AddScoped<ApiKeyHubFilter>();
        services.AddSignalR(options => options.AddFilter<ApiKeyHubFilter>());
        services.AddSingleton<IEventNotificationService, SignalREventNotificationService>();

        // ── Webhook Dispatcher & Real-time SignalR Bridge ─────────────────────────
        services.AddSingleton<WebhookDispatcher>();
        services.AddSingleton<IWebhookDispatcher, SignalRWebhookDispatcherBridge>();

        // ── WhatsApp Engine ───────────────────────────────────────────────────────
        services.AddSingleton<BaileysNetWhatsAppEngine>();
        services.AddSingleton<IWhatsAppEngine>(sp =>
            sp.GetRequiredService<BaileysNetWhatsAppEngine>());
        services.AddSingleton<InstanceRuntimeManager>();

        // ── Background Hosted Services ────────────────────────────────────────────
        services.AddHostedService<WebhookDeliveryWorker>();
        services.AddHostedService<OutboundMessageWorker>();
        services.AddHostedService<InboundStoreCleanerService>();
        services.AddHostedService<MultiTenantWhatsAppBackgroundWorker>();

        // ── Event Transport Dispatchers ───────────────────────────────────────────
        services.AddSingleton<RabbitMqEventDispatcher>();
        services.AddSingleton<SqsEventDispatcher>();
        services.AddSingleton<NatsEventDispatcher>();

        // ── OpenAI Processor ──────────────────────────────────────────────────────
        services.AddSingleton<OpenAiMessageProcessor>();

        // ── Rate Limiting ─────────────────────────────────────────────────────────
        services.AddInfrastructureRateLimiting(configuration);

        return services;
    }
}

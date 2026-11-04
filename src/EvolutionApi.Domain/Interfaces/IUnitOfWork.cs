namespace EvolutionApi.Domain.Interfaces;

/// <summary>
/// Unit of Work — coordinates multiple repositories in a single DB transaction.
/// </summary>
public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    IInstanceRepository Instances { get; }
    IWebhookRepository Webhooks { get; }
    IChatRepository Chats { get; }
    IMessageRepository Messages { get; }
    ISettingsRepository Settings { get; }
    IIntegrationRepository Integrations { get; }
    IScheduledMessageRepository ScheduledMessages { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task<bool> CanConnectAsync(CancellationToken ct = default);
    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}

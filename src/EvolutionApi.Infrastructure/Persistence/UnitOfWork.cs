using EvolutionApi.Domain.Interfaces;
using EvolutionApi.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace EvolutionApi.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of IUnitOfWork.
/// Coordinates transactions and repository lifecycles.
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _db;
    private IDbContextTransaction? _transaction;

    public IInstanceRepository Instances { get; }
    public IWebhookRepository Webhooks { get; }
    public IChatRepository Chats { get; }
    public IMessageRepository Messages { get; }
    public ISettingsRepository Settings { get; }
    public IIntegrationRepository Integrations { get; }
    public IScheduledMessageRepository ScheduledMessages { get; }

    public UnitOfWork(AppDbContext db)
    {
        _db = db;
        Instances = new InstanceRepository(db);
        Webhooks = new WebhookRepository(db);
        Chats = new ChatRepository(db);
        Messages = new MessageRepository(db);
        Settings = new SettingsRepository(db);
        Integrations = new IntegrationRepository(db);
        ScheduledMessages = new ScheduledMessageRepository(db);
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> CanConnectAsync(CancellationToken ct = default)
        => await _db.Database.CanConnectAsync(ct);

    public async Task BeginTransactionAsync(CancellationToken ct = default)
    {
        _transaction ??= await _db.Database.BeginTransactionAsync(ct);
    }

    public async Task CommitAsync(CancellationToken ct = default)
    {
        if (_transaction is not null)
        {
            await _transaction.CommitAsync(ct);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackAsync(CancellationToken ct = default)
    {
        if (_transaction is not null)
        {
            await _transaction.RollbackAsync(ct);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _transaction = null;
        _db.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
        await _db.DisposeAsync();
    }
}


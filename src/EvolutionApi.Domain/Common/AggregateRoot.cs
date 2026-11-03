namespace EvolutionApi.Domain.Common;

// ── Domain Event Marker ───────────────────────────────────────────────────────

/// <summary>
/// Marker interface for all domain events.
/// Domain events are immutable facts that describe something that happened
/// in the domain. They are raised by aggregates and handled asynchronously.
/// </summary>
public interface IDomainEvent
{
    /// <summary>Unique event identifier for idempotency and tracing.</summary>
    Guid EventId { get; }

    /// <summary>Timestamp when the event occurred (UTC).</summary>
    DateTime OccurredAt { get; }
}

// ── Aggregate Root ────────────────────────────────────────────────────────────

/// <summary>
/// Base class for aggregate roots.
/// Aggregates collect domain events during their lifetime; the Application layer
/// dispatches them after the Unit of Work commits.
/// </summary>
public abstract class AggregateRoot : BaseEntity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>Read-only view of pending domain events.</summary>
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>Registers a new domain event to be dispatched after persistence.</summary>
    protected void RaiseDomainEvent(IDomainEvent domainEvent)
        => _domainEvents.Add(domainEvent);

    /// <summary>Clears all pending domain events (called by the Unit of Work after dispatch).</summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}

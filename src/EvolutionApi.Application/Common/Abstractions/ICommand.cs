using MediatR;

namespace EvolutionApi.Application.Common.Abstractions;

// ── Command markers (write-side) ──────────────────────────────────────────────

/// <summary>
/// Marker for a command that returns a value of type <typeparamref name="TResult"/>.
/// Use for write operations that produce a meaningful result (e.g. create → returns created entity DTO).
/// </summary>
public interface ICommand<out TResult> : IRequest<TResult> { }

/// <summary>
/// Marker for a fire-and-forget command that returns nothing.
/// Use for delete / audit / void mutations.
/// </summary>
public interface ICommand : IRequest<Unit> { }

// ── Query markers (read-side) ─────────────────────────────────────────────────

/// <summary>
/// Marker for a query that returns a value.
/// Queries MUST be side-effect free and should be cacheable.
/// </summary>
public interface IQuery<out TResult> : IRequest<TResult> { }

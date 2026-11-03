namespace EvolutionApi.Application.Common.Abstractions;

// ── Application-layer typed errors ────────────────────────────────────────────

/// <summary>
/// Strongly typed application error. Replaces raw exceptions thrown from handlers,
/// keeping handler logic pure, deterministic, and unit-testable without try/catch.
/// </summary>
/// <param name="Code">Machine-readable error code (e.g. "NOT_FOUND", "CONFLICT").</param>
/// <param name="Message">Human-readable description.</param>
/// <param name="HttpStatus">Suggested HTTP status code for the API layer to map to.</param>
public sealed record AppError(string Code, string Message, int HttpStatus = 500)
{
    // ── Canonical factory methods ─────────────────────────────────────────────

    public static AppError NotFound(string resource, string identifier) =>
        new("NOT_FOUND", $"{resource} '{identifier}' was not found.", 404);

    public static AppError Conflict(string message) =>
        new("CONFLICT", message, 409);

    public static AppError Validation(string message) =>
        new("VALIDATION_FAILED", message, 400);

    public static AppError Unauthorized(string message) =>
        new("UNAUTHORIZED", message, 401);

    public static AppError Forbidden(string message) =>
        new("FORBIDDEN", message, 403);

    public static AppError BusinessRule(string message) =>
        new("BUSINESS_RULE_VIOLATION", message, 422);

    public static AppError Internal(string message) =>
        new("INTERNAL_ERROR", message, 500);
}

// ── Result<T> discriminated union ─────────────────────────────────────────────

/// <summary>
/// Discriminated union representing either a successful value or a typed <see cref="AppError"/>.
/// Handlers return this instead of throwing exceptions for expected failure cases.
/// </summary>
public sealed class Result<T>
{
    private readonly T? _value;
    private readonly AppError? _error;

    private Result(T value)      { _value = value; IsSuccess = true; }
    private Result(AppError err) { _error = err;   IsSuccess = false; }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException($"Cannot access Value of a failed Result: {_error!.Message}");

    public AppError Error => IsFailure
        ? _error!
        : throw new InvalidOperationException("Cannot access Error of a successful Result.");

    // ── Implicit conversions for ergonomic handler code ────────────────────────
    public static implicit operator Result<T>(T value)      => new(value);
    public static implicit operator Result<T>(AppError err) => new(err);

    // ── Functional combinators ─────────────────────────────────────────────────

    /// <summary>Maps the success value, leaving errors untouched.</summary>
    public Result<TOut> Map<TOut>(Func<T, TOut> mapper) =>
        IsSuccess ? Result<TOut>.Ok(mapper(_value!)) : Result<TOut>.Fail(_error!);

    /// <summary>Executes one of two delegates depending on success/failure.</summary>
    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<AppError, TOut> onFailure) =>
        IsSuccess ? onSuccess(_value!) : onFailure(_error!);

    // ── Static factory shortcuts ───────────────────────────────────────────────
    public static Result<T> Ok(T value)     => new(value);
    public static Result<T> Fail(AppError e) => new(e);
}

/// <summary>Non-generic Result for void commands.</summary>
public sealed class Result
{
    private readonly AppError? _error;

    private Result()              { IsSuccess = true; }
    private Result(AppError err)  { _error = err; IsSuccess = false; }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    public AppError Error => IsFailure
        ? _error!
        : throw new InvalidOperationException("Cannot access Error of a successful Result.");

    public static Result Ok()            => new();
    public static Result Fail(AppError e) => new(e);

    public static implicit operator Result(AppError err) => new(err);

    public TOut Match<TOut>(Func<TOut> onSuccess, Func<AppError, TOut> onFailure) =>
        IsSuccess ? onSuccess() : onFailure(_error!);
}

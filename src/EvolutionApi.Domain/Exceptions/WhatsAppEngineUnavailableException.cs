namespace EvolutionApi.Domain.Exceptions;

// ── WhatsApp Engine ───────────────────────────────────────────────────────────

/// <summary>
/// Thrown when the WhatsApp engine is unavailable or an operation
/// is not supported by the underlying engine adapter.
/// </summary>
public sealed class WhatsAppEngineUnavailableException : Exception
{
    public WhatsAppEngineUnavailableException(string message)
        : base(message) { }

    public WhatsAppEngineUnavailableException(string message, Exception innerException)
        : base(message, innerException) { }
}

// ── Instance State ────────────────────────────────────────────────────────────

/// <summary>
/// Thrown when an operation is attempted on a WhatsApp instance
/// that is not in the required lifecycle state (e.g. sending a message to a disconnected instance).
/// </summary>
public sealed class InstanceNotConnectedException : Exception
{
    public string InstanceName { get; }

    public InstanceNotConnectedException(string instanceName)
        : base($"Instance '{instanceName}' is not connected. Connect it first.")
        => InstanceName = instanceName;
}

// ── Tenant ────────────────────────────────────────────────────────────────────

/// <summary>
/// Thrown when a multi-tenant operation is attempted without a valid tenant context.
/// </summary>
public sealed class TenantContextMissingException : Exception
{
    public TenantContextMissingException()
        : base("No tenant context could be resolved for the current request.") { }
}

// ── Domain Rule Violation ─────────────────────────────────────────────────────

/// <summary>
/// Thrown when a domain invariant or business rule is violated.
/// Use in domain entities and services for rule enforcement.
/// </summary>
public sealed class DomainRuleViolationException : Exception
{
    /// <summary>Machine-readable rule code (e.g. "MAX_INSTANCES_REACHED").</summary>
    public string RuleCode { get; }

    public DomainRuleViolationException(string ruleCode, string message)
        : base(message)
        => RuleCode = ruleCode;
}

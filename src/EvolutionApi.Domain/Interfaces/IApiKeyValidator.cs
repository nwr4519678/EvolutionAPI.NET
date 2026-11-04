namespace EvolutionApi.Domain.Interfaces;

/// <summary>
/// Encapsulates the API key header and route data passed from the presentation layer.
/// </summary>
public sealed record ApiKeyValidationRequest(
    string? ApiKey,
    string? InstanceName = null,
    string? RoutePath = null);

/// <summary>
/// Result of an API key validation check, with optional error message.
/// </summary>
public sealed record ApiKeyValidationResult(bool IsValid, string? ErrorMessage = null)
{
    public static ApiKeyValidationResult Success() => new(true);
    public static ApiKeyValidationResult Failure(string message) => new(false, message);

    public static implicit operator bool(ApiKeyValidationResult result) => result.IsValid;
}

/// <summary>
/// Domain abstraction for API key authorization and validation.
/// </summary>
public interface IApiKeyValidator
{
    /// <summary>
    /// Validates an API key request containing header and route data.
    /// </summary>
    Task<ApiKeyValidationResult> ValidateApiKeyAsync(
        ApiKeyValidationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Convenience overload for positional parameters.
    /// </summary>
    Task<ApiKeyValidationResult> ValidateApiKeyAsync(
        string? suppliedApiKey,
        string? instanceName = null,
        string? requestPath = null,
        CancellationToken cancellationToken = default)
        => ValidateApiKeyAsync(new ApiKeyValidationRequest(suppliedApiKey, instanceName, requestPath), cancellationToken);
}

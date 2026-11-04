using System.Security.Cryptography;
using System.Text;
using EvolutionApi.Domain.Interfaces;
using FluentValidation;
using Microsoft.Extensions.Configuration;

namespace EvolutionApi.Infrastructure.Security;

/// <summary>
/// FluentValidation validator for API key authorization requests.
/// Evaluates presence, constant-time global matching, and database-backed instance hashes.
/// </summary>
public sealed class ApiKeyValidationRequestValidator : AbstractValidator<ApiKeyValidationRequest>
{
    private readonly IConfiguration _configuration;
    private readonly IInstanceRepository _instances;

    public ApiKeyValidationRequestValidator(
        IConfiguration configuration,
        IInstanceRepository instances)
    {
        _configuration = configuration;
        _instances = instances;

        // 1. Structural / Header existence checks
        RuleFor(x => x.ApiKey)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Invalid or missing apikey header")
            .Must(key => !string.IsNullOrWhiteSpace(key))
            .WithMessage("Invalid or missing apikey header");

        // 2. Cryptographic and data-driven authorization checks
        RuleFor(x => x)
            .MustAsync(AuthorizeKeyAsync)
            .WithMessage("Invalid or missing apikey header");
    }

    private async Task<bool> AuthorizeKeyAsync(ApiKeyValidationRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ApiKey))
            return false;

        var configured = _configuration["Evolution:ApiKey"] ?? _configuration["AUTHENTICATION_API_KEY"];

        // Rule A: Global configured API key check (constant-time comparison prevents timing attacks)
        if (!string.IsNullOrEmpty(configured) &&
            CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(request.ApiKey),
                Encoding.UTF8.GetBytes(configured)))
        {
            return true;
        }

        var suppliedHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.ApiKey)));

        // Rule B: Instance-specific API key hash check (from route data)
        if (!string.IsNullOrEmpty(request.InstanceName))
        {
            var instance = await _instances.GetByNameAsync(request.InstanceName, cancellationToken);
            if (instance is not null && instance.ApiKeyHash.Equals(suppliedHash, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        // Rule C: Endpoint-specific bypass for fetching all instances
        if (string.Equals(request.RoutePath, "/instance/fetchInstances", StringComparison.OrdinalIgnoreCase))
        {
            var instance = await _instances.GetByApiKeyHashAsync(suppliedHash, cancellationToken);
            if (instance is not null)
            {
                return true;
            }
        }

        return false;
    }
}

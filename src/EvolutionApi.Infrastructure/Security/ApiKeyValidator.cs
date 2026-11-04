using EvolutionApi.Domain.Interfaces;
using FluentValidation;

namespace EvolutionApi.Infrastructure.Security;

/// <summary>
/// Infrastructure service implementing IApiKeyValidator via FluentValidation.
/// </summary>
public sealed class ApiKeyValidator(IValidator<ApiKeyValidationRequest> validator) : IApiKeyValidator
{
    public async Task<ApiKeyValidationResult> ValidateApiKeyAsync(
        ApiKeyValidationRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await validator.ValidateAsync(request, cancellationToken);
        if (result.IsValid)
        {
            return ApiKeyValidationResult.Success();
        }

        var error = result.Errors.FirstOrDefault()?.ErrorMessage ?? "Invalid or missing apikey header";
        return ApiKeyValidationResult.Failure(error);
    }
}

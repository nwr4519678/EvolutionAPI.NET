using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace EvolutionApi.API.Startup;

/// <summary>
/// Validates the minimum configuration required to run the gateway safely in a
/// production environment. Keeping this outside Program.cs preserves a small,
/// declarative application composition root.
/// </summary>
public static class ProductionConfigurationExtensions
{
    private static readonly string[] RequiredSettings =
    [
        "ConnectionStrings:DefaultConnection",
        "Evolution:ApiKey",
        "Evolution:HangfireDashboardApiKey",
        "DataProtection:KeyPath"
    ];

    public static WebApplicationBuilder ValidateProductionConfiguration(this WebApplicationBuilder builder)
    {
        if (builder.Environment.IsDevelopment())
            return builder;

        var missing = RequiredSettings
            .Where(key => string.IsNullOrWhiteSpace(builder.Configuration[key]))
            .ToArray();
        if (missing.Length > 0)
            throw new InvalidOperationException(
                $"Missing required production configuration: {string.Join(", ", missing)}.");

        return builder;
    }
}

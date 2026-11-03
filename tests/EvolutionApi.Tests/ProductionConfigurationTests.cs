using EvolutionApi.API.Startup;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace EvolutionApi.Tests;

public sealed class ProductionConfigurationTests
{
    [Fact]
    public void ValidateProductionConfiguration_RejectsMissingRequiredSettings()
    {
        var builder = CreateProductionBuilder(new Dictionary<string, string?>());

        var exception = Assert.Throws<InvalidOperationException>(() => builder.ValidateProductionConfiguration());

        Assert.Contains("Evolution:ApiKey", exception.Message);
    }

    [Fact]
    public void ValidateProductionConfiguration_AcceptsCompleteSettings()
    {
        var builder = CreateProductionBuilder(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\MSSQLLocalDB;Database=Tests;Trusted_Connection=True",
            ["Evolution:ApiKey"] = "gateway-key",
            ["Evolution:HangfireDashboardApiKey"] = "dashboard-key",
            ["DataProtection:KeyPath"] = Path.GetTempPath()
        });

        var result = builder.ValidateProductionConfiguration();

        Assert.Same(builder, result);
    }

    private static WebApplicationBuilder CreateProductionBuilder(IDictionary<string, string?> values)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Production,
            ApplicationName = typeof(Program).Assembly.FullName
        });
        builder.Configuration.AddInMemoryCollection(values);
        return builder;
    }
}

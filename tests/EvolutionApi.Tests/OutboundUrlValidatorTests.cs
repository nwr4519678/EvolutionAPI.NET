using EvolutionApi.Infrastructure.Security;
using Microsoft.Extensions.Configuration;

namespace EvolutionApi.Tests;

public sealed class OutboundUrlValidatorTests
{
    [Theory]
    [InlineData("http://127.0.0.1/admin")]
    [InlineData("http://10.0.0.8/internal")]
    [InlineData("http://169.254.169.254/latest/meta-data")]
    [InlineData("ftp://files.example.com/a")]
    [InlineData("https://user:password@example.com/a")]
    public async Task IsAllowedAsync_RejectsUnsafeOrUnsupportedDestinations(string url)
    {
        var validator = CreateValidator();

        var allowed = await validator.IsAllowedAsync(url, CancellationToken.None);

        Assert.False(allowed);
    }

    [Fact]
    public async Task IsAllowedAsync_RejectsAHostOutsideConfiguredAllowList()
    {
        var validator = CreateValidator(new Dictionary<string, string?>
        {
            ["OutboundHttp:AllowedHosts"] = "hooks.example.com"
        });

        var allowed = await validator.IsAllowedAsync("https://cdn.example.com/message", CancellationToken.None);

        Assert.False(allowed);
    }

    private static OutboundUrlValidator CreateValidator(IDictionary<string, string?>? values = null) =>
        new(new ConfigurationBuilder().AddInMemoryCollection(values ?? new Dictionary<string, string?>()).Build());
}

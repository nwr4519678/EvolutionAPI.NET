using EvolutionApi.Infrastructure.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging.Abstractions;

namespace EvolutionApi.Tests;

public sealed class SecretProtectionTests
{
    [Fact]
    public void ProtectAndUnprotect_RoundTripsSessionMaterial()
    {
        var keyDirectory = Path.Combine(Path.GetTempPath(), "evolution-api-tests", Guid.NewGuid().ToString("N"));
        try
        {
            var provider = DataProtectionProvider.Create(new DirectoryInfo(keyDirectory), builder =>
                builder.SetApplicationName("EvolutionApi.Tests"));
            var protector = new DataProtectionSecretProtector(provider,
                NullLogger<DataProtectionSecretProtector>.Instance);

            var encrypted = protector.Protect("session-secret");

            Assert.NotEqual("session-secret", encrypted);
            Assert.Equal("session-secret", protector.Unprotect(encrypted));
        }
        finally
        {
            if (Directory.Exists(keyDirectory)) Directory.Delete(keyDirectory, recursive: true);
        }
    }
}

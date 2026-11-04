using System.Security.Cryptography;
using System.Text;
using Hangfire.Annotations;
using Hangfire.Dashboard;

namespace EvolutionApi.Infrastructure.Security;

/// <summary>
/// Denies the Hangfire dashboard unless a dedicated dashboard key is configured
/// and supplied in the <c>apikey</c> header.  A missing configuration must never
/// make the management surface public.
/// </summary>
public sealed class HangfireDashboardAuthorizationFilter(IConfiguration configuration) : IDashboardAuthorizationFilter
{
    public bool Authorize([NotNull] DashboardContext context)
    {
        var expected = configuration["Evolution:HangfireDashboardApiKey"];
        var supplied = context.GetHttpContext().Request.Headers["apikey"].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(expected) || string.IsNullOrWhiteSpace(supplied))
            return false;

        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var suppliedBytes = Encoding.UTF8.GetBytes(supplied);
        return expectedBytes.Length == suppliedBytes.Length &&
               CryptographicOperations.FixedTimeEquals(expectedBytes, suppliedBytes);
    }
}

using System.Net;
using System.Net.Sockets;

namespace EvolutionApi.Infrastructure.Security;

/// <summary>
/// Prevents user-configured webhooks and media URLs from reaching loopback,
/// private, link-local, or otherwise non-public network addresses.
/// Network egress rules must enforce the same boundary at the host level.
/// </summary>
public sealed class OutboundUrlValidator(IConfiguration configuration)
{
    private readonly HashSet<string> _allowedHosts = (configuration["OutboundHttp:AllowedHosts"] ?? string.Empty)
        .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    public async Task<bool> IsAllowedAsync(string? value, CancellationToken ct)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp) ||
            !string.IsNullOrEmpty(uri.UserInfo))
            return false;

        var host = uri.DnsSafeHost;
        if (_allowedHosts.Count > 0 && !_allowedHosts.Any(allowed =>
                host.Equals(allowed, StringComparison.OrdinalIgnoreCase) ||
                host.EndsWith('.' + allowed, StringComparison.OrdinalIgnoreCase)))
            return false;

        try
        {
            var addresses = IPAddress.TryParse(host, out var literal)
                ? [literal]
                : await Dns.GetHostAddressesAsync(host).WaitAsync(ct);
            return addresses.Length > 0 && addresses.All(IsPublicAddress);
        }
        catch (SocketException) { return false; }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { return false; }
    }

    private static bool IsPublicAddress(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
        if (IPAddress.IsLoopback(address) || address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any) ||
            address.Equals(IPAddress.IPv6None) || address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6Multicast)
            return false;

        var bytes = address.GetAddressBytes();
        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var a = bytes[0];
            var b = bytes[1];
            return a != 0 && a != 10 && a != 127 &&
                   !(a == 100 && b is >= 64 and <= 127) &&
                   !(a == 169 && b == 254) &&
                   !(a == 172 && b is >= 16 and <= 31) &&
                   !(a == 192 && b == 168);
        }

        // IPv6 unique-local addresses are fc00::/7.
        return (bytes[0] & 0xfe) != 0xfc;
    }
}

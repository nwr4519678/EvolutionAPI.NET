using EvolutionApi.Infrastructure.WhatsApp.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;

namespace EvolutionApi.Infrastructure.Security;

/// <summary>
/// Cryptographic secret protector backed by Microsoft.AspNetCore.DataProtection.
/// Encrypts Baileys authentication state, tokens, and cryptographic keys per tenant
/// to guarantee tenant isolation and confidentiality on shared hosting environments.
/// </summary>
public sealed class DataProtectionSecretProtector : ISecretProtector
{
    private readonly IDataProtectionProvider _protectionProvider;
    private readonly IDataProtector _defaultProtector;
    private readonly ILogger<DataProtectionSecretProtector> _logger;

    public DataProtectionSecretProtector(
        IDataProtectionProvider protectionProvider,
        ILogger<DataProtectionSecretProtector> logger)
    {
        _protectionProvider = protectionProvider;
        _logger = logger;
        _defaultProtector = _protectionProvider.CreateProtector("EvolutionApi.WhatsApp.GlobalSecrets.v1");
    }

    public string Protect(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return _defaultProtector.Protect(value);
    }

    public string Unprotect(string protectedValue)
    {
        if (string.IsNullOrEmpty(protectedValue)) return protectedValue;
        try
        {
            return _defaultProtector.Unprotect(protectedValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unprotect secret token using Microsoft.AspNetCore.DataProtection");
            throw;
        }
    }

    public string ProtectTenantSecret(string value, string tenantId)
    {
        if (string.IsNullOrEmpty(value)) return value;
        var tenantProtector = _protectionProvider.CreateProtector($"EvolutionApi.WhatsApp.TenantSecrets.v1:{tenantId}");
        return tenantProtector.Protect(value);
    }

    public string UnprotectTenantSecret(string protectedValue, string tenantId)
    {
        if (string.IsNullOrEmpty(protectedValue)) return protectedValue;
        var tenantProtector = _protectionProvider.CreateProtector($"EvolutionApi.WhatsApp.TenantSecrets.v1:{tenantId}");
        return tenantProtector.Unprotect(protectedValue);
    }
}

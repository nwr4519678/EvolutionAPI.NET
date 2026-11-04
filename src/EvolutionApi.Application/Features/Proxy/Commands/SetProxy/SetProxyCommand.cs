using EvolutionApi.Application.Common.Abstractions;
using EvolutionApi.Application.Features.Proxy.Caching;
using EvolutionApi.Domain.Entities;
using EvolutionApi.Domain.Interfaces;
using FluentValidation;

namespace EvolutionApi.Application.Features.Proxy.Commands.SetProxy;

// ── Command ───────────────────────────────────────────────────────────────────

public sealed record SetProxyCommand(
    string  InstanceName,
    bool    Enabled  = false,
    string  Host     = "",
    string  Port     = "",
    string  Protocol = "",
    string? Username = null,
    string? Password = null) : ICommand<SetProxyResult>;

// ── Response DTO ──────────────────────────────────────────────────────────────

public sealed record SetProxyResult(
    string InstanceName,
    bool   Enabled,
    string Host,
    string Port,
    string Protocol);

// ── Validator ─────────────────────────────────────────────────────────────────

public sealed class SetProxyCommandValidator : AbstractValidator<SetProxyCommand>
{
    public SetProxyCommandValidator()
    {
        RuleFor(x => x.InstanceName).NotEmpty().WithMessage("instanceName is required");
        RuleFor(x => x.Host).NotEmpty().WithMessage("host is required");
        RuleFor(x => x.Port)
            .NotEmpty().WithMessage("port is required")
            .Matches(@"^\d{1,5}$").WithMessage("port must be a valid port number");
        RuleFor(x => x.Protocol)
            .NotEmpty().WithMessage("protocol is required")
            .Must(p => p is "http" or "https" or "socks4" or "socks5")
            .WithMessage("protocol must be one of: http, https, socks4, socks5");
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class SetProxyCommandHandler(
    IInstanceRepository instances,
    ISettingsRepository settings,
    ICacheService cache) : MediatR.IRequestHandler<SetProxyCommand, SetProxyResult>
{
    public async Task<SetProxyResult> Handle(SetProxyCommand cmd, CancellationToken ct)
    {
        var instance = await instances.GetByNameAsync(cmd.InstanceName, ct)
            ?? throw new KeyNotFoundException($"Instance '{cmd.InstanceName}' not found.");

        var proxy = await settings.GetProxyAsync(instance.Id, ct)
            ?? new InstanceProxy { InstanceId = instance.Id };

        proxy.Host              = cmd.Host;
        proxy.Port              = cmd.Port;
        proxy.Protocol          = cmd.Protocol;
        proxy.Username          = cmd.Username ?? proxy.Username;
        proxy.PasswordEncrypted = cmd.Password ?? proxy.PasswordEncrypted;
        proxy.UpdatedAt         = DateTime.UtcNow;

        await settings.AddOrUpdateProxyAsync(proxy, ct);
        await cache.RemoveByTagAsync(ProxyCacheKeys.Tag(cmd.InstanceName), ct);

        return new SetProxyResult(cmd.InstanceName, cmd.Enabled, cmd.Host, cmd.Port, cmd.Protocol);
    }
}

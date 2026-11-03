namespace EvolutionApi.Application.Features.Proxy.DTOs;

public sealed record SetProxyDto(
    bool Enabled = false,
    string Host = "",
    string Port = "",
    string Protocol = "",
    string? Username = null,
    string? Password = null);

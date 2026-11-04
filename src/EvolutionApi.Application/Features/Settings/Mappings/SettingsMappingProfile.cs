using AutoMapper;
using EvolutionApi.Application.Features.Settings.Commands.SetSettings;
using EvolutionApi.Application.Features.Settings.DTOs;

namespace EvolutionApi.Application.Features.Settings.Mappings;

/// <summary>
/// AutoMapper profile for the Settings feature.
/// InstanceName is injected via ctx.Items["instanceName"].
/// </summary>
public sealed class SettingsMappingProfile : Profile
{
    public SettingsMappingProfile()
    {
        CreateMap<SetSettingsDto, SetSettingsCommand>()
            .ConstructUsing((dto, ctx) =>
            {
                var instanceName = ctx.Items.TryGetValue("instanceName", out var v)
                    ? v?.ToString() ?? string.Empty : string.Empty;

                return new SetSettingsCommand(
                    instanceName,
                    dto.RejectCall,
                    dto.MsgCall,
                    dto.GroupsIgnore,
                    dto.AlwaysOnline,
                    dto.ReadMessages,
                    dto.ReadStatus,
                    dto.SyncFullHistory,
                    dto.WavoipToken);
            });
    }
}

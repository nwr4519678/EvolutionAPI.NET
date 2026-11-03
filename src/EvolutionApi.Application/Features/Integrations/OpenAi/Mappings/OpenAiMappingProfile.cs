using AutoMapper;
using EvolutionApi.Application.Features.Integrations.OpenAi.Commands.SetOpenAi;
using EvolutionApi.Application.Features.Integrations.OpenAi.Commands.StartOpenAi;
using EvolutionApi.Application.Features.Integrations.OpenAi.DTOs;

namespace EvolutionApi.Application.Features.Integrations.OpenAi.Mappings;

/// <summary>
/// AutoMapper profile for the OpenAI integration feature.
/// All commands receive InstanceName via ctx.Items["instanceName"].
/// </summary>
public sealed class OpenAiMappingProfile : Profile
{
    public OpenAiMappingProfile()
    {
        CreateMap<SetOpenAiDto, SetOpenAiCommand>()
            .ConstructUsing((dto, ctx) =>
            {
                var instance = GetInstanceName(ctx);
                return new SetOpenAiCommand(instance, dto.Enabled, dto.ApiKey, dto.Model,
                    dto.SystemPrompt, dto.MaxTokens, dto.Temperature, dto.SpeechToText);
            });

        CreateMap<StartOpenAiDto, StartOpenAiCommand>()
            .ConstructUsing((dto, ctx) =>
                new StartOpenAiCommand(GetInstanceName(ctx), dto.RemoteJid));
    }

    private static string GetInstanceName(ResolutionContext ctx) =>
        ctx.Items.TryGetValue("instanceName", out var v) ? v?.ToString() ?? string.Empty : string.Empty;
}

using AutoMapper;
using EvolutionApi.Application.Features.Integrations.Chatwoot.Commands.SetChatwoot;
using EvolutionApi.Application.Features.Integrations.Chatwoot.DTOs;

namespace EvolutionApi.Application.Features.Integrations.Chatwoot.Mappings;

/// <summary>
/// AutoMapper profile for the Chatwoot integration feature.
/// InstanceName is injected via ctx.Items["instanceName"].
/// </summary>
public sealed class ChatwootMappingProfile : Profile
{
    public ChatwootMappingProfile()
    {
        CreateMap<SetChatwootDto, SetChatwootCommand>()
            .ConstructUsing((dto, ctx) =>
            {
                var instanceName = ctx.Items.TryGetValue("instanceName", out var v)
                    ? v?.ToString() ?? string.Empty : string.Empty;

                return new SetChatwootCommand(
                    instanceName,
                    dto.Enabled,
                    dto.AccountId,
                    dto.Token,
                    dto.Url,
                    dto.NameInbox,
                    dto.AutoCreate,
                    dto.ImportContacts,
                    dto.ImportMessages,
                    dto.DaysLimitImportMessages,
                    dto.SignMsg,
                    dto.ReOpenConversation,
                    dto.ConversationPending,
                    dto.MergeBrazilContacts);
            });
    }
}

using EvolutionApi.Domain.Entities;

namespace EvolutionApi.Domain.Interfaces;

/// <summary>
/// Third-party integration configurations repository contract.
/// </summary>
public interface IIntegrationRepository
{
    Task<TypebotSetting?> GetTypebotAsync(string instanceId, CancellationToken ct = default);
    Task AddOrUpdateTypebotAsync(TypebotSetting setting, CancellationToken ct = default);

    Task<ChatwootSetting?> GetChatwootAsync(string instanceId, CancellationToken ct = default);
    Task AddOrUpdateChatwootAsync(ChatwootSetting setting, CancellationToken ct = default);

    Task<OpenAiSetting?> GetOpenAiAsync(string instanceId, CancellationToken ct = default);
    Task AddOrUpdateOpenAiAsync(OpenAiSetting setting, CancellationToken ct = default);

    Task<RabbitMqSetting?> GetRabbitMqAsync(string instanceId, CancellationToken ct = default);
    Task AddOrUpdateRabbitMqAsync(RabbitMqSetting setting, CancellationToken ct = default);

    Task<SqsSetting?> GetSqsAsync(string instanceId, CancellationToken ct = default);
    Task AddOrUpdateSqsAsync(SqsSetting setting, CancellationToken ct = default);

    Task<NatsSetting?> GetNatsAsync(string instanceId, CancellationToken ct = default);
    Task AddOrUpdateNatsAsync(NatsSetting setting, CancellationToken ct = default);

    // Typebot sessions
    Task<TypebotSession?> GetTypebotSessionAsync(string instanceId, string remoteJid, CancellationToken ct = default);
    Task AddOrUpdateTypebotSessionAsync(TypebotSession session, CancellationToken ct = default);

    // OpenAI sessions
    Task<OpenAiSession?> GetOpenAiSessionAsync(string instanceId, string remoteJid, CancellationToken ct = default);
    Task AddOrUpdateOpenAiSessionAsync(OpenAiSession session, CancellationToken ct = default);
}

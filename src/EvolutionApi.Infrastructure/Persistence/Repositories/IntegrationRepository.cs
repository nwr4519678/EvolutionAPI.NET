using EvolutionApi.Domain.Entities;
using EvolutionApi.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EvolutionApi.Infrastructure.Persistence.Repositories;

/// <summary>
/// Third-party integration configurations repository implementation.
/// </summary>
public sealed class IntegrationRepository(AppDbContext db) : IIntegrationRepository
{
    public async Task<TypebotSetting?> GetTypebotAsync(string instanceId, CancellationToken ct = default)
        => await db.TypebotSettings.AsNoTracking().FirstOrDefaultAsync(t => t.InstanceId == instanceId, ct);

    public async Task AddOrUpdateTypebotAsync(TypebotSetting setting, CancellationToken ct = default)
    {
        var existing = await db.TypebotSettings.FirstOrDefaultAsync(t => t.InstanceId == setting.InstanceId, ct);
        if (existing is null) await db.TypebotSettings.AddAsync(setting, ct);
        else
        {
            existing.Enabled = setting.Enabled;
            existing.Url = setting.Url;
            existing.TypebotId = setting.TypebotId;
            existing.Expire = setting.Expire;
            existing.KeywordFinish = setting.KeywordFinish;
            existing.DelayMessage = setting.DelayMessage;
            existing.UnknownMessage = setting.UnknownMessage;
            existing.ListeningFromMe = setting.ListeningFromMe;
            existing.UpdatedAt = DateTime.UtcNow;
        }
    }

    public async Task<ChatwootSetting?> GetChatwootAsync(string instanceId, CancellationToken ct = default)
        => await db.ChatwootSettings.AsNoTracking().FirstOrDefaultAsync(c => c.InstanceId == instanceId, ct);

    public async Task AddOrUpdateChatwootAsync(ChatwootSetting setting, CancellationToken ct = default)
    {
        var existing = await db.ChatwootSettings.FirstOrDefaultAsync(c => c.InstanceId == setting.InstanceId, ct);
        if (existing is null) await db.ChatwootSettings.AddAsync(setting, ct);
        else
        {
            existing.Enabled = setting.Enabled;
            existing.AccountId = setting.AccountId;
            existing.Token = setting.Token;
            existing.Url = setting.Url;
            existing.NameInbox = setting.NameInbox;
            existing.AutoCreate = setting.AutoCreate;
            existing.ImportContacts = setting.ImportContacts;
            existing.ImportMessages = setting.ImportMessages;
            existing.DaysLimitImportMessages = setting.DaysLimitImportMessages;
            existing.SignMsg = setting.SignMsg;
            existing.ReOpenConversation = setting.ReOpenConversation;
            existing.ConversationPending = setting.ConversationPending;
            existing.MergeBrazilContacts = setting.MergeBrazilContacts;
            existing.UpdatedAt = DateTime.UtcNow;
        }
    }

    public async Task<OpenAiSetting?> GetOpenAiAsync(string instanceId, CancellationToken ct = default)
        => await db.OpenAiSettings.AsNoTracking().FirstOrDefaultAsync(o => o.InstanceId == instanceId, ct);

    public async Task AddOrUpdateOpenAiAsync(OpenAiSetting setting, CancellationToken ct = default)
    {
        var existing = await db.OpenAiSettings.FirstOrDefaultAsync(o => o.InstanceId == setting.InstanceId, ct);
        if (existing is null) await db.OpenAiSettings.AddAsync(setting, ct);
        else
        {
            existing.Enabled = setting.Enabled;
            existing.ApiKey = setting.ApiKey;
            existing.Model = setting.Model;
            existing.SystemPrompt = setting.SystemPrompt;
            existing.MaxTokens = setting.MaxTokens;
            existing.Temperature = setting.Temperature;
            existing.SpeechToText = setting.SpeechToText;
            existing.UpdatedAt = DateTime.UtcNow;
        }
    }

    public async Task<RabbitMqSetting?> GetRabbitMqAsync(string instanceId, CancellationToken ct = default)
        => await db.RabbitMqSettings.AsNoTracking().FirstOrDefaultAsync(r => r.InstanceId == instanceId, ct);

    public async Task AddOrUpdateRabbitMqAsync(RabbitMqSetting setting, CancellationToken ct = default)
    {
        var existing = await db.RabbitMqSettings.FirstOrDefaultAsync(r => r.InstanceId == setting.InstanceId, ct);
        if (existing is null) await db.RabbitMqSettings.AddAsync(setting, ct);
        else
        {
            existing.Enabled = setting.Enabled;
            existing.Uri = setting.Uri;
            existing.Exchange = setting.Exchange;
            existing.GlobalEvents = setting.GlobalEvents;
            existing.EventsJson = setting.EventsJson;
            existing.UpdatedAt = DateTime.UtcNow;
        }
    }

    public async Task<SqsSetting?> GetSqsAsync(string instanceId, CancellationToken ct = default)
        => await db.SqsSettings.AsNoTracking().FirstOrDefaultAsync(s => s.InstanceId == instanceId, ct);

    public async Task AddOrUpdateSqsAsync(SqsSetting setting, CancellationToken ct = default)
    {
        var existing = await db.SqsSettings.FirstOrDefaultAsync(s => s.InstanceId == setting.InstanceId, ct);
        if (existing is null) await db.SqsSettings.AddAsync(setting, ct);
        else
        {
            existing.Enabled = setting.Enabled;
            existing.AccessKeyId = setting.AccessKeyId;
            existing.SecretAccessKey = setting.SecretAccessKey;
            existing.AccountId = setting.AccountId;
            existing.Region = setting.Region;
            existing.EventsJson = setting.EventsJson;
            existing.UpdatedAt = DateTime.UtcNow;
        }
    }

    public async Task<NatsSetting?> GetNatsAsync(string instanceId, CancellationToken ct = default)
        => await db.NatsSettings.AsNoTracking().FirstOrDefaultAsync(n => n.InstanceId == instanceId, ct);

    public async Task AddOrUpdateNatsAsync(NatsSetting setting, CancellationToken ct = default)
    {
        var existing = await db.NatsSettings.FirstOrDefaultAsync(n => n.InstanceId == setting.InstanceId, ct);
        if (existing is null) await db.NatsSettings.AddAsync(setting, ct);
        else
        {
            existing.Enabled = setting.Enabled;
            existing.Uri = setting.Uri;
            existing.EventsJson = setting.EventsJson;
            existing.UpdatedAt = DateTime.UtcNow;
        }
    }

    // ── Typebot Sessions ──────────────────────────────────────────────────────

    public async Task<TypebotSession?> GetTypebotSessionAsync(string instanceId, string remoteJid, CancellationToken ct = default)
        => await db.TypebotSessions.AsNoTracking()
            .FirstOrDefaultAsync(s => s.InstanceId == instanceId && s.RemoteJid == remoteJid, ct);

    public async Task AddOrUpdateTypebotSessionAsync(TypebotSession session, CancellationToken ct = default)
    {
        var existing = await db.TypebotSessions
            .FirstOrDefaultAsync(s => s.InstanceId == session.InstanceId && s.RemoteJid == session.RemoteJid, ct);
        if (existing is null) await db.TypebotSessions.AddAsync(session, ct);
        else
        {
            existing.SessionId = session.SessionId;
            existing.Status    = session.Status;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        await db.SaveChangesAsync(ct);
    }

    // ── OpenAI Sessions ───────────────────────────────────────────────────────

    public async Task<OpenAiSession?> GetOpenAiSessionAsync(string instanceId, string remoteJid, CancellationToken ct = default)
        => await db.OpenAiSessions.AsNoTracking()
            .FirstOrDefaultAsync(s => s.InstanceId == instanceId && s.RemoteJid == remoteJid, ct);

    public async Task AddOrUpdateOpenAiSessionAsync(OpenAiSession session, CancellationToken ct = default)
    {
        var existing = await db.OpenAiSessions
            .FirstOrDefaultAsync(s => s.InstanceId == session.InstanceId && s.RemoteJid == session.RemoteJid, ct);
        if (existing is null) await db.OpenAiSessions.AddAsync(session, ct);
        else
        {
            existing.Status    = session.Status;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        await db.SaveChangesAsync(ct);
    }
}

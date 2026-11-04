using EvolutionApi.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;

namespace EvolutionApi.Infrastructure.Services;

/// <summary>
/// Processes incoming WhatsApp messages through the OpenAI Chat Completions API.
/// Uses the official OpenAI NuGet package (joint Microsoft/OpenAI SDK).
/// Configuration (API key, model, system prompt) is loaded per-instance from the database.
/// </summary>
public sealed class OpenAiMessageProcessor(
    IServiceScopeFactory scopeFactory,
    ILogger<OpenAiMessageProcessor> logger)
{
    /// <summary>
    /// Sends <paramref name="userMessage"/> to the configured OpenAI model and returns the reply text.
    /// Returns null when OpenAI is not enabled for this instance.
    /// </summary>
    public async Task<string?> ProcessAsync(string instanceId, string userMessage, CancellationToken ct = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var integrations = scope.ServiceProvider.GetRequiredService<IIntegrationRepository>();

        var cfg = await integrations.GetOpenAiAsync(instanceId, ct);
        if (cfg is null || !cfg.Enabled || string.IsNullOrWhiteSpace(cfg.ApiKey))
        {
            logger.LogDebug("OpenAI not configured or disabled for instance {InstanceId}", instanceId);
            return null;
        }

        try
        {
            // Official OpenAI .NET SDK — ChatClient is thread-safe and allocation-efficient
            var client = new ChatClient(
                model: cfg.Model ?? "gpt-4o-mini",
                apiKey: cfg.ApiKey);

            var messages = new List<ChatMessage>();

            if (!string.IsNullOrWhiteSpace(cfg.SystemPrompt))
                messages.Add(new SystemChatMessage(cfg.SystemPrompt));

            messages.Add(new UserChatMessage(userMessage));

            var options = new ChatCompletionOptions
            {
                MaxOutputTokenCount = cfg.MaxTokens > 0 ? cfg.MaxTokens : 1024,
                Temperature         = (float)(cfg.Temperature > 0 ? cfg.Temperature : 1.0)
            };

            var completion = await client.CompleteChatAsync(messages, options, ct);
            var reply = completion.Value.Content[0].Text;

            logger.LogInformation("OpenAI reply for instance {InstanceId}: {Length} chars", instanceId, reply.Length);
            return reply;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "OpenAI processing failed for instance {InstanceId}", instanceId);
            return null;
        }
    }
}

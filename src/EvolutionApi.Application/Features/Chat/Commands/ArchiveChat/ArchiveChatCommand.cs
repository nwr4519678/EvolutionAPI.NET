using EvolutionApi.Application.Common.Abstractions;
using EvolutionApi.Domain.Interfaces;
using FluentValidation;

namespace EvolutionApi.Application.Features.Chat.Commands.ArchiveChat;

// ── Command ───────────────────────────────────────────────────────────────────

public sealed record ArchiveChatCommand(
    string InstanceName,
    string Chat    = "",
    bool   Archive = true) : ICommand<ArchiveChatResult>;

// ── Response ──────────────────────────────────────────────────────────────────

public sealed record ArchiveChatResult(string Chat, bool Archived);

// ── Validator ─────────────────────────────────────────────────────────────────

public sealed class ArchiveChatCommandValidator : AbstractValidator<ArchiveChatCommand>
{
    public ArchiveChatCommandValidator()
    {
        RuleFor(x => x.InstanceName).NotEmpty().WithMessage("instanceName is required");
        RuleFor(x => x.Chat).NotEmpty().WithMessage("chat is required");
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────

public sealed class ArchiveChatCommandHandler(
    IInstanceRepository instances,
    IChatRepository chats) : MediatR.IRequestHandler<ArchiveChatCommand, ArchiveChatResult>
{
    public async Task<ArchiveChatResult> Handle(ArchiveChatCommand cmd, CancellationToken ct)
    {
        var instance = await instances.GetByNameAsync(cmd.InstanceName, ct)
            ?? throw new KeyNotFoundException($"Instance '{cmd.InstanceName}' not found.");

        await chats.ArchiveChatAsync(instance.Id, cmd.Chat, cmd.Archive, ct);

        return new ArchiveChatResult(cmd.Chat, cmd.Archive);
    }
}

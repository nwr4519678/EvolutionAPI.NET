namespace EvolutionApi.Application.Features.Labels.DTOs;

public sealed record CreateLabelDto(
    string Name = "",
    string Color = "#2196F3");

public sealed record UpdateLabelDto(
    string LabelId = "",
    string? Name = null,
    string? Color = null);

public sealed record DeleteLabelDto(
    string LabelId = "");

public sealed record HandleLabelDto(
    string Label = "",
    string ChatId = "",
    string Action = "add");

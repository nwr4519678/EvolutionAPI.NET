using EvolutionApi.Domain.Enums;
using System.Text.Json.Nodes;

namespace EvolutionApi.Application.Features.Messages.DTOs;

public sealed record SendTextMessageDto(
    string Number,
    string Text,
    int? Delay = null,
    string? QuotedMessageId = null,
    string[]? Mentions = null);

public sealed record SendMediaMessageDto(
    string Number,
    string Media, // base64 or URL
    string? MediaType = null, // image, video, document, audio
    string? Caption = null,
    string? FileName = null,
    string? MimeType = null,
    int? Delay = null);

public sealed record SendLocationMessageDto(
    string Number,
    double Latitude,
    double Longitude,
    string? Name = null,
    string? Address = null);

public sealed record SendContactMessageDto(
    string Number,
    string FullName,
    string WaNumber);

public sealed record SendAgnosticMessageDto(
    string DestinationPhone,
    AgnosticMessageType MessageType,
    string? Text = null,
    string? MediaUrlOrBase64 = null,
    string? Caption = null,
    string? FileName = null,
    string? MimeType = null,
    double? Latitude = null,
    double? Longitude = null,
    string? ContactName = null,
    string? ContactPhone = null);

public sealed record SendWhatsAppAudioDto(
    string Number,
    string Audio,
    int? Delay = null,
    string? MimeType = null);

public sealed record SendRawMessageDto(
    string Operation,
    System.Text.Json.Nodes.JsonNode? Payload);

public sealed record MessageResponseDto(
    string Key,
    string RemoteJid,
    string Status,
    DateTime TimestampUtc);



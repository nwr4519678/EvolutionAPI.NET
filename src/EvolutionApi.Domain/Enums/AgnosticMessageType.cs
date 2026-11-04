namespace EvolutionApi.Domain.Enums;

/// <summary>
/// Agnostic payload message types supported by the WhatsApp gateway.
/// </summary>
public enum AgnosticMessageType
{
    Text = 1,
    MediaImage = 2,
    MediaVideo = 3,
    MediaAudio = 4,
    MediaDocument = 5,
    Location = 6,
    Contact = 7,
    Poll = 8,
    List = 9,
    Reaction = 10
}

namespace Application.DTOs.Auth.Response;

/// <summary>
/// Response for magic-link request flow.
/// </summary>
public sealed class MagicLinkResponse(string message, string? magicLink = null)
{
    public string Message { get; init; } = message;
    public string? MagicLink { get; init; } = magicLink;
}

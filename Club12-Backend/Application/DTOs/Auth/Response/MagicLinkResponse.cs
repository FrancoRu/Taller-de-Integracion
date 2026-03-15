using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.Auth.Response;

/// <summary>
/// Response for magic-link request flow.
/// </summary>
/// <remarks>
/// In production you should typically NOT return the link; send it via email.
/// </remarks>
public sealed class MagicLinkResponse(string message, string? magicLink = null)
{
    public string Message { get; init; } = message;
    public string? MagicLink { get; init; } = magicLink;
}

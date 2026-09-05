using System;
namespace Application.DTOs.Player.Request;

/// <summary>
/// Represents a request to get filtered players for admin access.
/// </summary>
public class GetPlayersFilteredRequest : PlayerFilterRequestBase
{
    public string? DocumentNumber { get; set; }

    public DateTime? BirthDate { get; set; }

    public string? PhoneNumber { get; set; }

    public string? SocialSecurity { get; set; }

    public bool? IsFederated { get; set; }

    public string? Club { get; set; }

    public string? Category { get; set; }
}

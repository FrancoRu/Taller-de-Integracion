using Application.DTOs.Abstract.Response;

using Domain.Enums;

using System;
namespace Application.DTOs.PlayerSanction.Response;

/// <summary>
/// Represents a response for a Player Sanction.
/// </summary>
public class PlayerSanctionResponse : BaseEntityResponse
{
    /// <summary>
    /// The duration in fixtures, or fechas, of the sanction.
    /// </summary>
    public required int Duration { get; set; }

    /// <summary>
    /// The number of fechas still to be served; zero means fully served, null means it cannot be computed.
    /// </summary>
    public int? FechasRemaining { get; set; }

    /// <summary>
    /// Whether the sanction is still active, meaning fechas remain to be served.
    /// </summary>
    public bool IsActive { get; set; }

    public required DateTime IssuedDate { get; set; }

    public required string Description { get; set; }

    /// <summary>
    /// The unique, URL-friendly identifier used in sanction links.
    /// </summary>
    public required string Slug { get; set; }

    /// <summary>
    /// The kind of subject the sanction targets: Player, Team, or Staff.
    /// </summary>
    public SanctionSubjectType SubjectType { get; set; }

    /// <summary>
    /// The sanctioned player's identifier; null for team or staff sanctions.
    /// </summary>
    public Guid? PlayerId { get; set; }

    /// <summary>
    /// The sanctioned player's full name. Null for team or staff sanctions.
    /// </summary>
    public string? PlayerFullName { get; set; }

    /// <summary>
    /// The sanctioned team's identifier; null unless this is a team sanction.
    /// </summary>
    public Guid? TeamId { get; set; }

    /// <summary>
    /// The sanctioned team's name. Null unless this is a team sanction.
    /// </summary>
    public string? TeamName { get; set; }

    /// <summary>
    /// The sanctioned staff member's name. Null unless this is a staff sanction.
    /// </summary>
    public string? StaffName { get; set; }

    public required Guid MatchId { get; set; }

    public SanctionAppealStatus AppealStatus { get; set; }

    public string? AppealReason { get; set; }

    public DateTime? AppealDate { get; set; }

    public string? AppealResolution { get; set; }

    public DateTime? AppealResolvedDate { get; set; }
}

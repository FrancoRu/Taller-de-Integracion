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
    /// The duration in fixtures (fechas / jornadas) of the sanction.
    /// </summary>
    public required int Duration { get; set; }

    /// <summary>
    /// The number of FECHAS (jornadas) still to be served, computed from the
    /// team's finished rounds since the sanction was issued (HU-75). Zero means
    /// the sanction has been fully served. Null when it cannot be computed
    /// (e.g. the originating match has no round). This is expressed in fechas,
    /// never in calendar days.
    /// </summary>
    public int? FechasRemaining { get; set; }

    /// <summary>
    /// Whether the sanction is still active (HU-76): true while there are
    /// fechas remaining to be served. A fully-served sanction is inactive.
    /// </summary>
    public bool IsActive { get; set; }

    public required DateTime IssuedDate { get; set; }

    public required string Description { get; set; }

    /// <summary>
    /// The unique, URL-friendly identifier used in sanction links.
    /// </summary>
    public required string Slug { get; set; }

    /// <summary>
    /// The kind of subject the sanction targets (HU-77): Player, Team or Staff.
    /// </summary>
    public SanctionSubjectType SubjectType { get; set; }

    /// <summary>
    /// The unique identifier of the player who has the sanction. Null for
    /// team or staff sanctions.
    /// </summary>
    public Guid? PlayerId { get; set; }

    /// <summary>
    /// The sanctioned player's full name. Null for team or staff sanctions.
    /// </summary>
    public string? PlayerFullName { get; set; }

    /// <summary>
    /// The unique identifier of the sanctioned team. Null unless this is a
    /// team sanction.
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

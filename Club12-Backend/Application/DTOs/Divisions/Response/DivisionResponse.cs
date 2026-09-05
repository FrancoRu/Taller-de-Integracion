using Domain.Enums;

using System;
using System.Collections.Generic;
namespace Application.DTOs.Divisions.Response;

/// <summary>
/// Represents the response structure for a division, 
/// including details about the division, its matches, and positions.
/// </summary>
public class DivisionResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The unique, URL-friendly identifier used in public division links.
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    public bool IsFinished { get; set; }

    /// <summary>
    /// For a multi-group cross-division cup this is the pooled union across
    /// every internal group (so the team counter reflects all groups' teams);
    /// use <see cref="GroupStandings"/> to render one table per group.
    /// </summary>
    public List<PositionResponse>? Positions { get; set; }

    /// <summary>
    /// One standings table per Group stage (HU-110). A regular zone has a
    /// single entry; a multi-group cross-division cup has one per internal
    /// group ("Grupo 1".."Grupo N"). Null/empty when the division has no
    /// Group stage yet.
    /// </summary>
    public List<GroupStandingsResponse>? GroupStandings { get; set; }

    public Guid TournamentId { get; set; }

    /// <summary>
    /// The parent tournament's slug, when its Tournament navigation was
    /// loaded; null otherwise. Lets callers build a clean `/torneos/{slug}`
    /// link back to the tournament instead of falling back to its GUID.
    /// </summary>
    public string? TournamentSlug { get; set; }

    /// <summary>
    /// Whether this division is a cross-division cup that intentionally
    /// draws teams from every other division in the tournament.
    /// </summary>
    public bool IsCrossDivisionCup { get; set; }

    /// <summary>
    /// Competitive category (gender) of the division (HU-48). Always matches
    /// the parent tournament's category.
    /// </summary>
    public TournamentCategory Category { get; set; }

    /// <summary>Points awarded for a win in this division's standings (HU-79).</summary>
    public int PointsForWin { get; set; }

    /// <summary>Points awarded for a loss in this division's standings (HU-79).</summary>
    public int PointsForLoss { get; set; }

    /// <summary>
    /// How many teams qualify to the bracket from EACH internal group of a
    /// multi-group cross-division cup (HU-110). Defaults to 1.
    /// </summary>
    public int QualifiersPerGroup { get; set; }

    /// <summary>
    /// The division's position-range → playoff-destination mapping (HU-45).
    /// </summary>
    public List<PlayoffMappingResponse>? PlayoffMappings { get; set; }

    /// <summary>
    /// The standings-position ranges that qualify to a playoff cup (HU-45),
    /// ordered top-down (0 = top cup). Derived from <see cref="PlayoffMappings"/>
    /// so the public standings table can highlight the qualifying rows and show
    /// a per-cup legend. Empty when the division has no playoff mappings.
    /// </summary>
    public List<QualificationRangeResponse>? QualificationRanges { get; set; }
}

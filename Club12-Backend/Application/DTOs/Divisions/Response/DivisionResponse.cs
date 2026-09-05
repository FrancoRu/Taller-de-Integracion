using Domain.Enums;

using System;
using System.Collections.Generic;
namespace Application.DTOs.Divisions.Response;

/// <summary>
/// The response structure for a division, including its matches and positions.
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
    /// For a multi-group cup this is the pooled union across every group; use GroupStandings for per-group tables.
    /// </summary>
    public List<PositionResponse>? Positions { get; set; }

    /// <summary>
    /// One standings table per Group stage, null or empty when the division has no Group stage yet.
    /// </summary>
    public List<GroupStandingsResponse>? GroupStandings { get; set; }

    public Guid TournamentId { get; set; }

    /// <summary>
    /// The parent tournament's slug, when loaded; null otherwise, for building a clean tournament link.
    /// </summary>
    public string? TournamentSlug { get; set; }

    /// <summary>
    /// Whether this division is a cross-division cup that intentionally draws teams from every division.
    /// </summary>
    public bool IsCrossDivisionCup { get; set; }

    /// <summary>
    /// Competitive category of the division, always matching the parent tournament's category.
    /// </summary>
    public TournamentCategory Category { get; set; }

    /// <summary>
    /// Points awarded for a win in this division's standings.
    /// </summary>
    public int PointsForWin { get; set; }

    /// <summary>
    /// Points awarded for a loss in this division's standings.
    /// </summary>
    public int PointsForLoss { get; set; }

    /// <summary>
    /// How many teams qualify from each internal group of a multi-group cup; defaults to 1.
    /// </summary>
    public int QualifiersPerGroup { get; set; }

    /// <summary>
    /// The division's position-range to playoff-destination mapping.
    /// </summary>
    public List<PlayoffMappingResponse>? PlayoffMappings { get; set; }

    /// <summary>
    /// The standings-position ranges that qualify to a playoff cup, ordered top-down; empty when none exist.
    /// </summary>
    public List<QualificationRangeResponse>? QualificationRanges { get; set; }
}

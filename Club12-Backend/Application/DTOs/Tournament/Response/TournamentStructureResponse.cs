using Application.DTOs.Divisions.Response;

using Domain.Enums;

using System.Collections.Generic;

namespace Application.DTOs.Tournament.Response;

/// <summary>
/// A source tournament's pure structure tree (divisions, their stages and
/// playoff mappings) for the tournament-cloning wizard-prefill flow (HU-cloning).
/// Carries STRUCTURE ONLY — no rosters, matches, standings, sanctions, audit
/// logs, or DrawnAt timestamps — so it can never leak instance data into a
/// clone. Additive: never replaces TournamentResponse/DivisionResponse, which
/// stay unchanged for their existing unrelated pages.
/// </summary>
public class TournamentStructureResponse
{
    public required string Name { get; set; }

    public string? Description { get; set; }

    /// <summary>
    /// The source tournament's category, shown for reference only — the clone
    /// action always requires the organizer to choose the new category
    /// explicitly, never inheriting this value silently.
    /// </summary>
    public required TournamentCategory Category { get; set; }

    public required List<DivisionStructureResponse> Divisions { get; set; }
}

/// <summary>
/// One division's cloneable structure: its scoring/cup configuration, playoff
/// mappings, and full Stage list — everything the reverse-mapper needs to
/// reconstruct a `ZoneConfig` or `CrossCupConfig` (D1), and nothing else.
/// </summary>
public class DivisionStructureResponse
{
    public required string Name { get; set; }

    public bool IsCrossDivisionCup { get; set; }

    public int PointsForWin { get; set; }

    public int PointsForLoss { get; set; }

    public int QualifiersPerGroup { get; set; }

    public List<PlayoffMappingResponse> PlayoffMappings { get; set; } = [];

    public required List<StageStructureResponse> Stages { get; set; }
}

/// <summary>
/// One stage's cloneable shape: its type, ordering, and bracket/series
/// configuration. Carries no dates, no DrawnAt, and no match data.
/// </summary>
public class StageStructureResponse
{
    public required string Name { get; set; }

    /// <summary>
    /// Groups parallel elimination brackets under a cup name; null for a
    /// division's default/group stage.
    /// </summary>
    public string? BracketName { get; set; }

    public required StageType StageType { get; set; }

    public bool IsElimination { get; set; }

    public int Order { get; set; }

    public int BestOf { get; set; }

    public int RoundRobinLegs { get; set; }
}

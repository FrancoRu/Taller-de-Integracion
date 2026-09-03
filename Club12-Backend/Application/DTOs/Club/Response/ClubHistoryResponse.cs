using System;
using System.Collections.Generic;

namespace Application.DTOs.Club.Response;

/// <summary>
/// A club and its trajectory across seasons (HU-99): the stable club identity
/// plus every per-season <see cref="Domain.Entities.Models.Team"/> that belongs
/// to it, each with the tournaments (seasons) it was registered in.
/// </summary>
public class ClubHistoryResponse
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public string? LogoUrl { get; set; }

    /// <summary>The per-season teams that make up this club's history.</summary>
    public required List<ClubTeamSeasonResponse> Teams { get; set; } = [];
}

/// <summary>
/// One per-season <see cref="Domain.Entities.Models.Team"/> row belonging to a
/// club, together with the seasons it participated in.
/// </summary>
public class ClubTeamSeasonResponse
{
    public required Guid TeamId { get; set; }
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public required string ThreeLetterCode { get; set; }

    /// <summary>
    /// The tournaments (seasons) this team was registered in — the source of
    /// truth for participation is <see cref="Domain.Entities.Models.TeamTournamentRegistration"/>.
    /// </summary>
    public required List<ClubSeasonResponse> Seasons { get; set; } = [];
}

/// <summary>One season a club's team participated in.</summary>
public class ClubSeasonResponse
{
    public required Guid TournamentId { get; set; }
    public string? TournamentName { get; set; }

    /// <summary>
    /// The tournament's start date. Only a sort key for the history table
    /// (newest season first); not displayed. Defaults to
    /// <see cref="DateTime.MinValue"/> when the tournament cannot be resolved,
    /// so such rows sort last.
    /// </summary>
    public DateTime StartDate { get; set; }
}

using System;

namespace Application.DTOs.Champions.Response;

/// <summary>
/// One row of the champions history: the champion (1st place) of a single
/// division of a FINISHED tournament, with enough tournament, season and
/// category context to render a history table across seasons. Divisions whose
/// champion is not decided are never emitted.
/// </summary>
public class ChampionHistoryResponse
{
    /// <summary>The id of the tournament the champion was crowned in.</summary>
    public required Guid TournamentId { get; set; }

    /// <summary>The name of the tournament.</summary>
    public required string TournamentName { get; set; }

    /// <summary>
    /// The name of the season ("Temporada") the tournament belongs to, or null
    /// when the tournament is not grouped under any season.
    /// </summary>
    public string? SeasonName { get; set; }

    /// <summary>
    /// The calendar year of the season the tournament belongs to, or null when
    /// there is no season or the season has no year set. The public page sorts
    /// seasons by this value, newest first.
    /// </summary>
    public int? SeasonYear { get; set; }

    /// <summary>
    /// The competitive category (gender) of the division, e.g. "Masculine" or
    /// "Feminine".
    /// </summary>
    public required string Category { get; set; }

    /// <summary>The name of the division that was won.</summary>
    public required string DivisionName { get; set; }

    /// <summary>
    /// The name of the sub-cup (playoff bracket) that was won, e.g. "Copa Oro"
    /// or "Copa Plata", when the division splits its playoff into several cups
    /// by sub-tier. Null when the division crowns a single champion (a single
    /// bracket, a cross-division cup, or a group-only division), so the caller
    /// can omit a redundant cup label in that case.
    /// </summary>
    public string? CupName { get; set; }

    /// <summary>The team that won this cup (its 1st place).</summary>
    public required PodiumTeamResponse ChampionTeam { get; set; }
}

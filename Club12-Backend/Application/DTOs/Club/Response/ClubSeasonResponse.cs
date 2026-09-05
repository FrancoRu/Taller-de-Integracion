using System;

namespace Application.DTOs.Club.Response;

/// <summary>
/// One season a club's team participated in.
/// </summary>
public class ClubSeasonResponse
{
    public required Guid TournamentId { get; set; }
    public string? TournamentName { get; set; }

    /// <summary>
    /// Sort key only, not displayed; defaults to the minimum date so unresolved tournaments sort last.
    /// </summary>
    public DateTime StartDate { get; set; }
}

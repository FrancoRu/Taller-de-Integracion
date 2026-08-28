using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Player.Request;

/// <summary>
/// Request to register a player onto a team's roster for a tournament season,
/// optionally assigning a dorsal (HU-54).
/// </summary>
public class RegisterPlayerToTeamRequest
{
    /// <summary>The team to register the player onto.</summary>
    [Required]
    public required Guid TeamId { get; set; }

    /// <summary>The tournament (season) the registration belongs to.</summary>
    [Required]
    public required Guid TournamentId { get; set; }

    /// <summary>The player's jersey number (dorsal) for this team/season, or null.</summary>
    public int? JerseyNumber { get; set; }
}

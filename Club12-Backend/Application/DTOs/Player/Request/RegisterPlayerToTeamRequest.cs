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

    /// <summary>
    /// The player's jersey number (dorsal) for this team/season, or null. A
    /// dorsal is a two-digit number: 0 to 99 inclusive, never negative.
    /// </summary>
    [Range(0, 99, ErrorMessage = "El dorsal debe ser un número entre 0 y 99.")]
    public int? JerseyNumber { get; set; }
}

using Domain.Enums;

using System;
using System.ComponentModel.DataAnnotations;
namespace Application.DTOs.Tournament.Request;

/// <summary>
/// Represents a request to create a new tournament.
/// </summary>
public class CreateTournamentRequest
{
    /// <summary>
    /// The description of the tournament. Optional.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Name is required.")]
    public required string Name { get; set; }

    /// <summary>
    /// The deadline for team registrations; must be earlier than the tournament start date.
    /// </summary>
    [Required(ErrorMessage = "Team registration deadline is required.")]
    public required DateTime TeamRegistrationDeadline { get; set; }

    [Required(ErrorMessage = "Start date is required.")]
    public required DateTime StartDate { get; set; }

    /// <summary>
    /// Competitive category of the tournament; feminine and masculine competitions are separate tournaments.
    /// </summary>
    public TournamentCategory Category { get; set; } = TournamentCategory.Masculine;

    /// <summary>
    /// Optional id of the season this tournament belongs to; omitting it leaves the tournament ungrouped.
    /// </summary>
    public Guid? SeasonId { get; set; }
}

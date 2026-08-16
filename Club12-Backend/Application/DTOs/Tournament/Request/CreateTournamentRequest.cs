using Application.Utils.Constants.Validation;

using System;
using System.ComponentModel.DataAnnotations;
namespace Application.DTOs.Tournament.Request;

/// <summary>
/// Represents a request to create a new tournament.
/// </summary>
public class CreateTournamentRequest
{
    /// <summary>
    /// The description of the tournament.
    /// </summary>
    [Required(ErrorMessage = "Description is required.")]
    public required string Description { get; set; }

    /// <summary>
    /// The name of the tournament.
    /// </summary>
    [Required(ErrorMessage = "Name is required.")]
    public required string Name { get; set; }

    /// <summary>
    /// The deadline for team registrations.
    /// Must be earlier than the tournament start date.
    /// </summary>
    [Required(ErrorMessage = "Team registration deadline is required.")]
    public required DateTime TeamRegistrationDeadline { get; set; }

    /// <summary>
    /// The start date of the tournament.
    /// </summary>
    [Required(ErrorMessage = "Start date is required.")]
    public required DateTime StartDate { get; set; }

    /// <summary>
    /// The maximum number of teams allowed to participate in the tournament.
    /// </summary>
    [Required(ErrorMessage = "Max teams is required.")]
    [Range(TournamentFieldRange.MinAllowedTeams, TournamentFieldRange.MaxAllowedTeams,
        ErrorMessage = "Max teams must be between 4 and 32.")]
    public required int MaxTeams { get; set; }

    /// <summary>
    /// The minimum number of teams required to hold the tournament.
    /// </summary>
    [Required(ErrorMessage = "Min teams is required.")]
    [Range(TournamentFieldRange.MinAllowedTeams, TournamentFieldRange.MaxAllowedTeams,
        ErrorMessage = "Min teams must be between 4 and 32.")]
    public required int MinTeams { get; set; }
}

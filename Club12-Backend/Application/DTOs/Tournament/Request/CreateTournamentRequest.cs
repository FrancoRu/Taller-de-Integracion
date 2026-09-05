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
    /// The deadline for team registrations.
    /// Must be earlier than the tournament start date.
    /// </summary>
    [Required(ErrorMessage = "Team registration deadline is required.")]
    public required DateTime TeamRegistrationDeadline { get; set; }

    [Required(ErrorMessage = "Start date is required.")]
    public required DateTime StartDate { get; set; }

    /// <summary>
    /// Competitive category (gender) of the tournament (HU-48). The feminine
    /// competition is a separate tournament and cannot share a tournament with
    /// masculine divisions. Defaults to
    /// <see cref="TournamentCategory.Masculine"/> when omitted.
    /// </summary>
    public TournamentCategory Category { get; set; } = TournamentCategory.Masculine;

    /// <summary>
    /// Optional id of the season ("Temporada") this tournament belongs to. When
    /// supplied the tournament is grouped under that season; omitting it leaves
    /// the tournament ungrouped. Purely additive — it never affects
    /// <see cref="Category"/> (HU-48).
    /// </summary>
    public Guid? SeasonId { get; set; }
}

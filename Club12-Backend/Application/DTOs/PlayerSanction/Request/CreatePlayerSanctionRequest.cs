using Application.Utils.Constants.Validation;

using Domain.Enums;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.PlayerSanction.Request;

/// <summary>
/// Represents a request to create a Player Sanction.
/// </summary>
public class CreatePlayerSanctionRequest : IValidatableObject
{
    /// <summary>
    /// The duration in fixtures, or fechas, of the sanction.
    /// </summary>
    [Required(ErrorMessage = "The Duration field is required.")]
    public required int Duration { get; set; }

    [Required(ErrorMessage = "The IssuedDate field is required.")]
    public required DateTime IssuedDate { get; set; }

    [Required(ErrorMessage = "The Description field is required.")]
    [MaxLength(SanctionFieldLengths.DescriptionMaxLength)]
    public required string Description { get; set; }

    /// <summary>
    /// The kind of subject the sanction targets; defaults to Player so existing clients keep working.
    /// </summary>
    public SanctionSubjectType SubjectType { get; set; } = SanctionSubjectType.Player;

    /// <summary>
    /// The sanctioned player's identifier; required when SubjectType is Player.
    /// </summary>
    public Guid? PlayerId { get; set; }

    /// <summary>
    /// The sanctioned team's identifier; required when SubjectType is Team.
    /// </summary>
    public Guid? TeamId { get; set; }

    /// <summary>
    /// The sanctioned staff member's name; required when SubjectType is Staff.
    /// </summary>
    [MaxLength(SanctionFieldLengths.DescriptionMaxLength)]
    public string? StaffName { get; set; }

    public required Guid MatchId { get; set; }

    /// <summary>
    /// Ensures the identity that matches the chosen subject type is supplied.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        switch (SubjectType)
        {
            case SanctionSubjectType.Player when !PlayerId.HasValue:
                yield return new ValidationResult(
                    "The PlayerId field is required for a player sanction.",
                    [nameof(PlayerId)]);
                break;
            case SanctionSubjectType.Team when !TeamId.HasValue:
                yield return new ValidationResult(
                    "The TeamId field is required for a team sanction.",
                    [nameof(TeamId)]);
                break;
            case SanctionSubjectType.Staff when string.IsNullOrWhiteSpace(StaffName):
                yield return new ValidationResult(
                    "The StaffName field is required for a staff sanction.",
                    [nameof(StaffName)]);
                break;
        }
    }
}

using Domain.Enums;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Tournament.Request;

/// <summary>
/// The whole tournament-wizard payload, persisted atomically so a failure leaves no partial tournament behind.
/// </summary>
public class CreateFullTournamentRequest
{
    /// <summary>
    /// Optional; neither the create wizard nor the edit form require it.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Name is required.")]
    public required string Name { get; set; }

    [Required(ErrorMessage = "Team registration deadline is required.")]
    public required DateTime TeamRegistrationDeadline { get; set; }

    [Required(ErrorMessage = "Start date is required.")]
    public required DateTime StartDate { get; set; }

    /// <summary>
    /// Competitive category of the tournament; every division below must share this category.
    /// </summary>
    public TournamentCategory Category { get; set; } = TournamentCategory.Masculine;

    /// <summary>
    /// Optional id of the season this tournament belongs to; omitting it leaves the tournament ungrouped.
    /// </summary>
    public Guid? SeasonId { get; set; }

    /// <summary>
    /// The divisions to create under this tournament, each with its own stages and playoff mappings.
    /// </summary>
    public List<CreateFullDivisionRequest> Divisions { get; set; } = [];
}

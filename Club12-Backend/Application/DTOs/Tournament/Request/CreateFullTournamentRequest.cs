using Domain.Enums;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Tournament.Request;

/// <summary>
/// HU-38: the whole tournament-wizard payload persisted atomically by
/// <c>POST /api/tournaments/full</c>. Carries the tournament base fields plus
/// every division (zone/cross-cup) with its cups, points, playoff mappings and
/// stages. The entire graph is created in a single transaction, so a failure
/// leaves NO partial tournament behind.
/// </summary>
public class CreateFullTournamentRequest
{
    /// <summary>Optional — neither the create wizard nor the edit form require it.</summary>
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Name is required.")]
    public required string Name { get; set; }

    [Required(ErrorMessage = "Team registration deadline is required.")]
    public required DateTime TeamRegistrationDeadline { get; set; }

    [Required(ErrorMessage = "Start date is required.")]
    public required DateTime StartDate { get; set; }

    /// <summary>
    /// Competitive category (gender) of the tournament (HU-48). Every division
    /// below must share this category.
    /// </summary>
    public TournamentCategory Category { get; set; } = TournamentCategory.Masculine;

    /// <summary>
    /// Optional id of the season ("Temporada") this tournament belongs to. When
    /// supplied the tournament is grouped under that season; omitting it leaves
    /// the tournament ungrouped. Mirrors <see cref="CreateTournamentRequest.SeasonId"/>
    /// so the compound create keeps the tournament linked to its season the same
    /// way the incremental base create does.
    /// </summary>
    public Guid? SeasonId { get; set; }

    /// <summary>
    /// The divisions (zones and optional cross-division cup) to create under
    /// this tournament, each with its own stages and playoff mappings.
    /// </summary>
    public List<CreateFullDivisionRequest> Divisions { get; set; } = [];
}

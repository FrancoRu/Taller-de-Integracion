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
    [Required(ErrorMessage = "Description is required.")]
    public required string Description { get; set; }

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
    /// The divisions (zones and optional cross-division cup) to create under
    /// this tournament, each with its own stages and playoff mappings.
    /// </summary>
    public List<CreateFullDivisionRequest> Divisions { get; set; } = [];
}

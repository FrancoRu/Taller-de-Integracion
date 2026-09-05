using Application.DTOs.Abstract.Response;
using Application.DTOs.Divisions.Response;

using Domain.Enums;

using System;
using System.Collections.Generic;
namespace Application.DTOs.Tournament.Response;

/// <summary>
/// Represents the response containing details of a tournament.
/// </summary>
public class TournamentResponse : BaseEntityResponse
{
    public required string Description { get; set; }

    public required string Name { get; set; }

    /// <summary>
    /// The unique, URL-friendly identifier used in public tournament links.
    /// </summary>
    public required string Slug { get; set; }

    public required IEnumerable<MinimalDivisionResponse> Divisions { get; set; }

    /// <summary>
    /// The deadline for team registrations.
    /// Must be earlier than the tournament start date.
    /// </summary>
    public required DateTime TeamRegistrationDeadline { get; set; }

    public required DateTime StartDate { get; set; }

    public TournamentStatus Status { get; set; }

    /// <summary>
    /// Competitive category (gender) of the tournament (HU-48). The feminine
    /// competition is played as its own separate tournament.
    /// </summary>
    public TournamentCategory Category { get; set; }

    /// <summary>
    /// Id of the season ("Temporada") this tournament belongs to, or null when
    /// it is not grouped under any season.
    /// </summary>
    public Guid? SeasonId { get; set; }

    /// <summary>
    /// Name of the season this tournament belongs to, when the season is
    /// loaded; null otherwise.
    /// </summary>
    public string? SeasonName { get; set; }

    /// <summary>
    /// Slug of the season this tournament belongs to, when the season is
    /// loaded; null otherwise. Lets callers build a clean `/temporadas/{slug}`
    /// link back to the season instead of falling back to its GUID.
    /// </summary>
    public string? SeasonSlug { get; set; }
}

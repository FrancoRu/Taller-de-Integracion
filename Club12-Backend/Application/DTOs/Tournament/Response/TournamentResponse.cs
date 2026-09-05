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
    /// The deadline for team registrations; must be earlier than the tournament start date.
    /// </summary>
    public required DateTime TeamRegistrationDeadline { get; set; }

    public required DateTime StartDate { get; set; }

    public TournamentStatus Status { get; set; }

    /// <summary>
    /// Competitive category of the tournament; the feminine competition is played as its own tournament.
    /// </summary>
    public TournamentCategory Category { get; set; }

    /// <summary>
    /// Id of the season this tournament belongs to, or null when not grouped under any season.
    /// </summary>
    public Guid? SeasonId { get; set; }

    /// <summary>
    /// Name of the season this tournament belongs to, when loaded; null otherwise.
    /// </summary>
    public string? SeasonName { get; set; }

    /// <summary>
    /// Slug of the season this tournament belongs to, when loaded, for building a clean season link.
    /// </summary>
    public string? SeasonSlug { get; set; }
}

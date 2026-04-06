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
    /// <summary>
    /// The description of the tournament.
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// The name of the tournament.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// The divisions associated with the tournament.
    /// </summary>
    public required IEnumerable<MinimalDivisionResponse> Divisions { get; set; }

    /// <summary>
    /// The deadline for team registrations.
    /// Must be earlier than the tournament start date.
    /// </summary>
    public required DateTime TeamRegistrationDeadline { get; set; }

    /// <summary>
    /// The start date of the tournament.
    /// </summary>
    public required DateTime StartDate { get; set; }

    /// <summary>
    /// The maximum number of teams allowed to participate in the tournament.
    /// </summary>
    public required int MaxTeams { get; set; }

    /// <summary>
    /// The minimum number of teams required to hold the tournament.
    /// </summary>
    public required int MinTeams { get; set; }

    /// <summary>
    /// Current lifecycle status of the tournament.
    /// </summary>
    public TournamentStatus Status { get; set; }
}

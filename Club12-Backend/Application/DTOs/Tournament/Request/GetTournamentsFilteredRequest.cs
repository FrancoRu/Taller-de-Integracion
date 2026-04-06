using Application.DTOs.Abstract.Request;
using Domain.Enums;
using System;
namespace Application.DTOs.Tournament.Request;

/// <summary>
/// Represents a request to get filtered tournaments.
/// </summary>
public class GetTournamentsFilteredRequest : PaginatedFilterRequest
{
    /// <summary>
    /// The name of the tournament to filter by.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// The description of the tournament to filter by.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The tournament status to filter by.
    /// </summary>
    public TournamentStatus? Status { get; set; }
}

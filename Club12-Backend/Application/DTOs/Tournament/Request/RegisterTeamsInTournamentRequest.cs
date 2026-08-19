using System;
using System.Collections.Generic;
namespace Application.DTOs.Tournament.Request;

/// <summary>
/// Represents the request to register multiple teams in a tournament.
/// </summary>
public class RegisterTeamsInTournamentRequest
{
    /// <summary>
    /// The list of unique identifiers (GUIDs) of the teams to be registered in the tournament.
    /// </summary>
    public required List<Guid> TeamIds { get; set; }
}

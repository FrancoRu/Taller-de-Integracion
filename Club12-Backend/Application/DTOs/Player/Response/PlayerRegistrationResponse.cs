using System;

namespace Application.DTOs.Player.Response;

/// <summary>
/// The outcome of registering a player onto a team's roster for a season (HU-54).
/// </summary>
public class PlayerRegistrationResponse
{
    public required Guid PlayerId { get; set; }
    public required Guid TeamId { get; set; }
    public required Guid TournamentId { get; set; }
    public int? JerseyNumber { get; set; }
}

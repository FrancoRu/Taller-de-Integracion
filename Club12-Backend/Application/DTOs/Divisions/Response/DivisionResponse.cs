using System;
using System.Collections.Generic;
namespace Application.DTOs.Divisions.Response;

/// <summary>
/// Represents the response structure for a division, 
/// including details about the division, its matches, and positions.
/// </summary>
public class DivisionResponse
{
    /// <summary>
    /// Gets or sets the unique identifier of the division.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the division.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The unique, URL-friendly identifier used in public division links.
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the division has finished.
    /// </summary>
    public bool IsFinished { get; set; }

    /// <summary>
    /// Gets or sets the list of positions for teams in the division.
    /// </summary>
    public List<PositionResponse>? Positions { get; set; }

    /// <summary>
    /// Gets or sets the ID of the tournament to which the division belongs.
    /// </summary>
    public Guid TournamentId { get; set; }

    /// <summary>
    /// Whether this division is a cross-division cup that intentionally
    /// draws teams from every other division in the tournament.
    /// </summary>
    public bool IsCrossDivisionCup { get; set; }
}

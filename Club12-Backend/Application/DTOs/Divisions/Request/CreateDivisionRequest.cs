using System;
using System.ComponentModel.DataAnnotations;
namespace Application.DTOs.Divisions.Request;

/// <summary>
/// Represents a request to create a division.
/// </summary>
public class CreateDivisionRequest
{
    /// <summary>
    /// The name of the division.
    /// </summary>
    [Required(ErrorMessage = "The Name field is required.")]
    public required string Name { get; set; }

    /// <summary>
    /// The Id of the tournament this division belongs to.
    /// </summary>
    [Required(ErrorMessage = "The TournamentId field is required.")]
    public required Guid TournamentId { get; set; }

    /// <summary>
    /// Marks this division as a cross-division cup (e.g. "Copa Club12")
    /// that intentionally draws teams from every other division in the
    /// tournament, rather than being a team's single competitive tier.
    /// Defaults to false.
    /// </summary>
    public bool IsCrossDivisionCup { get; set; } = false;
}

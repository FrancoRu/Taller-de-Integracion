using Entities.DTOs.Abstract;
using Entities.DTOs.Divisions;

namespace Entities.DTOs.Tournament;

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
}

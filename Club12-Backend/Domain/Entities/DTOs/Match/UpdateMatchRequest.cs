namespace Entities.DTOs.Match;

/// <summary>
/// Represents a request to update a match.
/// </summary>
public class UpdateMatchRequest
{
    /// <summary>
    /// The new match date.
    /// </summary>
    public DateTime? MatchDate { get; set; }

    /// <summary>
    /// The id of the venue where the match will be played.
    /// </summary>
    public Guid? VenueId { get; set; }
}

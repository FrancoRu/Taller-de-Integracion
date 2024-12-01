using System.ComponentModel.DataAnnotations;

namespace Entities.DTOs.Match;


/// <summary>
/// Represents a request to update the score of a player.
/// </summary>
public class PlayerScoreRequest
{
    /// <summary>
    /// The ID of the player.
    /// </summary>
    [Required]
    public Guid PlayerId { get; set; }

    /// <summary>
    /// The points scored by the player.
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Player points must be a non-negative number.")]
    public int Points { get; set; }
}
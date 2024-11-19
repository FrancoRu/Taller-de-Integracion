using System.ComponentModel.DataAnnotations;

namespace Entities.DTOs.PlayerSanction;

/// <summary>
/// Represents a request to update a Player Sanction.
/// </summary>
public class UpdatePlayerSanctionRequest
{
    /// <summary>
    /// The duration in fixtures of the sanction.
    /// </summary>
    public int? Duration { get; set; }

    /// <summary>
    /// A description of the sanction.
    /// </summary>
    [MaxLength(255)]
    public string? Description { get; set; }
}

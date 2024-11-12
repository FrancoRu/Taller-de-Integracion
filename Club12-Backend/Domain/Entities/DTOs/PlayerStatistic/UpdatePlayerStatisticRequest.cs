using System.ComponentModel.DataAnnotations;

namespace Entities.DTOs.PlayerStatistic;

/// <summary>
/// Represents a request to update a player statistic.
/// </summary>
public class UpdatePlayerStatisticRequest
{
    /// <summary>
    /// The new value of the statistic.
    /// </summary>
    [Required]
    [Range(0, int.MaxValue, ErrorMessage = "Value must be a non-negative integer.")]
    public required int Value { get; set; }
}

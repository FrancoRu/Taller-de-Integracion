using System;
using System.ComponentModel.DataAnnotations;
namespace Application.DTOs.PlayerStatistic.Request;

/// <summary>
/// Represents a request to update a player statistic.
/// </summary>
public class UpdatePlayerStatisticRequest
{
    [Required]
    [Range(0, int.MaxValue, ErrorMessage = "Value must be a non-negative integer.")]
    public required int Value { get; set; }
}

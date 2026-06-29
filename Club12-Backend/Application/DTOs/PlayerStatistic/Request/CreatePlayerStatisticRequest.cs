using System;
using System.ComponentModel.DataAnnotations;
using Domain.Enums;
namespace Application.DTOs.PlayerStatistic.Request;

/// <summary>
/// Request DTO for creating a player statistic.
/// </summary>
public class CreatePlayerStatisticRequest
{
    /// <summary>
    /// The value of the statistic for the player.
    /// </summary>
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Value must be a non-negative number.")]
    public int Value { get; set; }

    /// <summary>
    /// The type of statistic (Points or Assists). Defaults to Points.
    /// </summary>
    public StatisticType Type { get; set; } = StatisticType.Points;

    /// <summary>
    /// The ID of the associated match.
    /// </summary>
    [Required]
    public Guid MatchId { get; set; }

    /// <summary>
    /// The ID of the associated player.
    /// </summary>
    [Required]
    public Guid PlayerId { get; set; }
}

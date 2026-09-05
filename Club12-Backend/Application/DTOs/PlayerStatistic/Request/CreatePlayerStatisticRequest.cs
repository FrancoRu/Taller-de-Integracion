using Domain.Enums;

using System;
using System.ComponentModel.DataAnnotations;
namespace Application.DTOs.PlayerStatistic.Request;

/// <summary>
/// Request DTO for creating a player statistic.
/// </summary>
public class CreatePlayerStatisticRequest
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Value must be a non-negative number.")]
    public int Value { get; set; }

    /// <summary>
    /// The type of statistic, Points or Assists, defaulting to Points.
    /// </summary>
    public StatisticType Type { get; set; } = StatisticType.Points;

    [Required]
    public Guid MatchId { get; set; }

    [Required]
    public Guid PlayerId { get; set; }
}

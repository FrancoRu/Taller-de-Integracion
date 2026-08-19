using Application.DTOs.Abstract.Response;

using Domain.Enums;

using System;
namespace Application.DTOs.PlayerStatistic.Response;

/// <summary>
/// Response DTO for player statistic.
/// </summary>
public class PlayerStatisticResponse : BaseEntityResponse
{
    /// <summary>
    /// The ID of the associated player.
    /// </summary>
    public Guid PlayerId { get; set; }

    /// <summary>
    /// The value of the statistic for the player.
    /// </summary>
    public int Value { get; set; }

    /// <summary>
    /// The type of statistic (Points or Assists).
    /// </summary>
    public StatisticType Type { get; set; }

    /// <summary>
    /// The ID of the associated match.
    /// </summary>
    public Guid MatchId { get; set; }

    /// <summary>
    /// The date of the associated match, for display without a separate lookup.
    /// </summary>
    public DateTime? MatchDate { get; set; }
}

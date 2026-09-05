using Application.DTOs.Abstract.Response;

using Domain.Enums;

using System;
namespace Application.DTOs.PlayerStatistic.Response;

/// <summary>
/// Response DTO for player statistic.
/// </summary>
public class PlayerStatisticResponse : BaseEntityResponse
{
    public Guid PlayerId { get; set; }

    public int Value { get; set; }

    /// <summary>
    /// The type of statistic, Points or Assists.
    /// </summary>
    public StatisticType Type { get; set; }

    public Guid MatchId { get; set; }

    /// <summary>
    /// The date of the associated match, for display without a separate lookup.
    /// </summary>
    public DateTime? MatchDate { get; set; }
}

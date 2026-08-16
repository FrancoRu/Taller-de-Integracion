using Application.DTOs.Abstract.Request;

using Domain.Enums;

using System;
namespace Application.DTOs.PlayerStatistic.Request;

/// <summary>
/// Represents a request to get filtered player statistics.
/// </summary>
public class GetPlayerStatisticsFilteredRequest : PaginatedFilterRequest
{
    /// <summary>
    /// The unique identifier of the match to filter statistics by.
    /// </summary>
    public Guid? MatchId { get; set; }

    /// <summary>
    /// The unique identifier of the player to filter statistics by.
    /// </summary>
    public Guid? PlayerId { get; set; }

    /// <summary>
    /// The unique identifier of the team to filter statistics by (matches
    /// the statistic's player's team).
    /// </summary>
    public Guid? TeamId { get; set; }

    /// <summary>
    /// The type of statistic to filter by (Points or Assists).
    /// </summary>
    public StatisticType? Type { get; set; }
}

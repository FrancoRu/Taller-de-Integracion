using Application.DTOs.Abstract.Response;

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
    /// The ID of the associated match.
    /// </summary>
    public Guid MatchId { get; set; }
}

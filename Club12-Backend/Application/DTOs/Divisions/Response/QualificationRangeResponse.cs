namespace Application.DTOs.Divisions.Response;

/// <summary>
/// One standings-position range that qualifies to a playoff cup (HU-45),
/// shaped for the public standings table so it can highlight the qualifying
/// rows and render a per-cup legend. Derived from the division's
/// <see cref="Domain.Entities.Models.DivisionPlayoffMapping"/> set.
/// </summary>
public class QualificationRangeResponse
{
    /// <summary>First standings position in the range (1-based, inclusive).</summary>
    public int FromPosition { get; set; }

    /// <summary>Last standings position in the range (1-based, inclusive).</summary>
    public int ToPosition { get; set; }

    /// <summary>The cup the teams in this range qualify for (e.g. "Copa Oro").</summary>
    public string CupName { get; set; } = string.Empty;

    /// <summary>
    /// The cup's rank, top-down: 0 is the top cup ("Copa Oro"), 1 the next,
    /// and so on. Drives the color the frontend paints each range.
    /// </summary>
    public int Order { get; set; }
}

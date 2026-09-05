namespace Application.DTOs.Divisions.Response;

/// <summary>
/// One standings-position range that qualifies to a playoff cup, derived from the division's mapping set.
/// </summary>
public class QualificationRangeResponse
{
    /// <summary>
    /// First standings position in the range, 1-based and inclusive.
    /// </summary>
    public int FromPosition { get; set; }

    /// <summary>
    /// Last standings position in the range, 1-based and inclusive.
    /// </summary>
    public int ToPosition { get; set; }

    /// <summary>
    /// The cup the teams in this range qualify for.
    /// </summary>
    public string CupName { get; set; } = string.Empty;

    /// <summary>
    /// The cup's rank, top-down, where 0 is the top cup; drives the color the frontend paints each range.
    /// </summary>
    public int Order { get; set; }
}

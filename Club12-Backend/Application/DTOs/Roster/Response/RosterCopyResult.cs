namespace Application.DTOs.Roster.Response;

/// <summary>
/// Outcome of copying a roster from a previous season into a new season.
/// </summary>
public class RosterCopyResult
{
    /// <summary>
    /// New season registrations created on the target team.
    /// </summary>
    public required int CopiedCount { get; set; }

    /// <summary>
    /// Source players skipped because they were already registered to the target season.
    /// </summary>
    public required int SkippedCount { get; set; }
}

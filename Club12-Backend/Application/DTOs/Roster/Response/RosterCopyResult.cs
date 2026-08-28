namespace Application.DTOs.Roster.Response;

/// <summary>
/// Outcome of copying a roster from a previous season into a new season (HU-53).
/// </summary>
public class RosterCopyResult
{
    /// <summary>New season registrations created on the target team.</summary>
    public required int CopiedCount { get; set; }

    /// <summary>
    /// Source players skipped because they were already registered to the
    /// target season (keeps the copy idempotent and honors the HU-54 rule that
    /// a player cannot be in two teams of the same tournament).
    /// </summary>
    public required int SkippedCount { get; set; }
}

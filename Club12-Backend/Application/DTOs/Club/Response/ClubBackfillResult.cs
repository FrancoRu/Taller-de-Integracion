namespace Application.DTOs.Club.Response;

/// <summary>
/// Outcome of the idempotent club backfill; a second run reports zeros.
/// </summary>
public class ClubBackfillResult
{
    public required int ClubsCreated { get; set; }
    public required int TeamsLinked { get; set; }
}

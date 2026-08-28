namespace Application.DTOs.Club.Response;

/// <summary>
/// Outcome of the idempotent club backfill (HU-99): how many stable
/// <see cref="Domain.Entities.Models.Club"/> rows were created and how many
/// previously-unlinked teams were linked to one. A second run reports zeros.
/// </summary>
public class ClubBackfillResult
{
    public required int ClubsCreated { get; set; }
    public required int TeamsLinked { get; set; }
}

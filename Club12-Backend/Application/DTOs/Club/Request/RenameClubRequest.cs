namespace Application.DTOs.Club.Request;

/// <summary>
/// Renames a club. The club's slug (used in its public URL) never changes, only its display name.
/// </summary>
public class RenameClubRequest
{
    public required string Name { get; set; }
}

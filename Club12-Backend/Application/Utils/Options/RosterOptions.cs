namespace Application.Utils.Options;

/// <summary>
/// Configurable roster limits.
/// </summary>
public class RosterOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Roster";

    /// <summary>
    /// Maximum number of players allowed in a single team's roster for one tournament season.
    /// </summary>
    public int MaxPlayersPerTeam { get; set; } = 25;
}

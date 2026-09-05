namespace Domain.Enums;

/// <summary>
/// Competitive gender category of a tournament, living on the Tournament as the source of truth every division must share.
/// </summary>
public enum TournamentCategory
{
    /// <summary>
    /// Masculine competition, the default for existing or unspecified tournaments.
    /// </summary>
    Masculine = 0,

    /// <summary>
    /// Feminine competition, played as its own separate tournament.
    /// </summary>
    Feminine = 1
}

namespace Domain.Enums;

/// <summary>
/// Competitive category (gender) of a tournament (HU-48). The feminine
/// competition is, by club rule, a SEPARATE tournament: a single tournament
/// can never mix feminine and masculine divisions. The category therefore
/// lives on the <see cref="Domain.Entities.Models.Tournament"/> as the source
/// of truth, and every one of its divisions must share it.
/// </summary>
public enum TournamentCategory
{
    /// <summary>Masculine competition. Default for existing/unspecified tournaments.</summary>
    Masculine = 0,

    /// <summary>Feminine competition, played as its own separate tournament.</summary>
    Feminine = 1
}

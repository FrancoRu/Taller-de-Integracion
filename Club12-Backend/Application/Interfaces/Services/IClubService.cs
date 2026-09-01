using Application.DTOs.Club.Response;

using Domain.Entities.Models;

using System.Threading.Tasks;

namespace Application.Interfaces.Services;

/// <summary>
/// Manages the stable, cross-season club identity (HU-99) that per-season
/// <see cref="Domain.Entities.Models.Team"/> rows hang off of.
/// </summary>
public interface IClubService
{
    /// <summary>
    /// Idempotently ensures every team is linked to a stable
    /// <see cref="Domain.Entities.Models.Club"/>: creates one club per distinct
    /// team Name (falling back to the three-letter code when a name is blank)
    /// and links each currently-unlinked team to it, so all the season teams of
    /// the same real-world club (e.g. "Colón SF" across seasons) resolve to one
    /// club. Teams that already have a <see cref="Domain.Entities.Models.Team.ClubId"/>
    /// are left untouched, so re-running changes nothing.
    /// </summary>
    /// <returns>How many clubs were created and teams linked (zeros on a re-run).</returns>
    Task<ClubBackfillResult> BackfillClubsAsync();

    /// <summary>
    /// Idempotently links a single team to its stable club (same name/slug
    /// match as <see cref="BackfillClubsAsync"/>), creating the club if this
    /// is the first team with that name. Called right after a team is
    /// created so "Importar plantel de una temporada anterior" (HU-53) has a
    /// club history to search from day one, instead of only after someone
    /// remembers to run the bulk backfill.
    /// </summary>
    /// <param name="team">A persisted team. Left untouched if already linked.</param>
    Task EnsureTeamLinkedToClubAsync(Team team);

    /// <summary>
    /// Returns a club and its trajectory across seasons: every per-season team
    /// that belongs to it and the tournaments each was registered in.
    /// </summary>
    /// <param name="idOrSlug">The club's GUID id or its slug.</param>
    /// <returns>The club history, or null when no club matches.</returns>
    Task<ClubHistoryResponse?> GetClubHistoryAsync(string idOrSlug);
}

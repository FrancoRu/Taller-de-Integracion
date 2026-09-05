using Application.DTOs.Club.Response;

using Domain.Entities.Models;

using System.Threading.Tasks;

namespace Application.Interfaces.Services;

/// <summary>
/// Manages the stable, cross-season club identity that per-season Team rows hang off of.
/// </summary>
public interface IClubService
{
    /// <summary>
    /// Idempotently ensures every team is linked to a stable Club, creating one club per distinct team Name and linking each currently-unlinked team to it.
    /// </summary>
    /// <returns>How many clubs were created and teams linked, zeros on a re-run.</returns>
    Task<ClubBackfillResult> BackfillClubsAsync();

    /// <summary>
    /// Idempotently links a single team to its stable club, creating the club if this is the first team with that name.
    /// </summary>
    /// <param name="team">A persisted team. Left untouched if already linked.</param>
    Task EnsureTeamLinkedToClubAsync(Team team);

    /// <summary>
    /// Returns a club and its trajectory across seasons: every per-season team that belongs to it and the tournaments each was registered in.
    /// </summary>
    /// <param name="idOrSlug">The club's GUID id or its slug.</param>
    /// <returns>The club history, or null when no club matches.</returns>
    Task<ClubHistoryResponse?> GetClubHistoryAsync(string idOrSlug);
}

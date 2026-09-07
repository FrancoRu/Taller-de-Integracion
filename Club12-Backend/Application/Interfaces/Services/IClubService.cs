using Application.DTOs.Club.Response;

using Domain.Entities.Models;

using System;
using System.Collections.Generic;
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

    /// <summary>
    /// Every club's stable identity summary, ordered by name — used to populate the "link to parent club" picker.
    /// </summary>
    Task<IEnumerable<ClubSummaryResponse>> GetAllClubsAsync();

    /// <summary>
    /// Links a club as a squad of a parent institution club. Flat and one level deep: rejects linking a club to itself, linking to a parent that is itself already a squad, and linking a club that already has its own squads.
    /// </summary>
    /// <param name="childClubId">The squad club to link.</param>
    /// <param name="parentClubId">The institution club it becomes a squad of.</param>
    Task<ClubHistoryResponse> LinkClubToParentAsync(Guid childClubId, Guid parentClubId);

    /// <summary>
    /// Clears a club's parent link, if any. Idempotent.
    /// </summary>
    /// <param name="childClubId">The club to unlink.</param>
    Task<ClubHistoryResponse> UnlinkClubParentAsync(Guid childClubId);

    /// <summary>
    /// Renames a club. The club's slug never changes, so its public URL stays stable.
    /// </summary>
    /// <param name="clubId">The club to rename.</param>
    /// <param name="name">The new display name.</param>
    Task<ClubHistoryResponse> RenameClubAsync(Guid clubId, string name);
}

using Application.DTOs.Abstract.Response;
using Application.DTOs.Team.Request;
using Application.DTOs.Team.Response;

using Domain.Entities.Models;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces.Services;

public interface ITeamService
{
    /// <summary>
    /// Creates a team, generates its unique slug from the name, and links it to its stable cross-season Club.
    /// </summary>
    /// <param name="teamEntity">The team entity to create.</param>
    /// <returns>The created team.</returns>
    Task<Team> CreateTeamAsync(Team teamEntity);

    /// <summary>
    /// Retrieves a team by its id, with its Players roster scoped to one season.
    /// </summary>
    /// <param name="teamId">The id of the team to retrieve.</param>
    /// <param name="tournamentId">
    /// The season whose roster to attach. Defaults to the team's own current TournamentId when omitted, so callers get today's roster by default. A team with no current tournament, TournamentId null, and no explicit tournamentId gets an empty roster.
    /// </param>
    /// <returns>The team with the specified id, or null if not found.</returns>
    Task<Team?> GetTeamByIdAsync(Guid teamId, Guid? tournamentId = null);

    /// <summary>
    /// Retrieves a team by its id or its slug, with its Players roster scoped to one season, treating the value as an id when it parses as a GUID and otherwise looking it up as a slug.
    /// </summary>
    /// <param name="idOrSlug">The team's GUID id or its slug.</param>
    /// <param name="tournamentId">
    /// The season whose roster to attach. Defaults to the team's own current
    /// TournamentId when omitted.
    /// </param>
    /// <returns>The matching team, or null if not found.</returns>
    Task<Team?> GetTeamByIdOrSlugAsync(string idOrSlug, Guid? tournamentId = null);

    Task UpdateTeamAsync(Team teamEntity);

    /// <summary>
    /// Guards a team's identity edit, freezing Team.Name and Team.ThreeLetterCode while the team is participating in an Ongoing tournament.
    /// </summary>
    /// <param name="existingTeam">The team as currently persisted, original identity and current TournamentId, read BEFORE the request is mapped over it.</param>
    /// <param name="requestedName">The requested new name, or null when the request does not change the name.</param>
    /// <param name="requestedThreeLetterCode">The requested new three-letter code, or null when the request does not change it.</param>
    /// <exception cref="System.InvalidOperationException">Thrown, mapped to 409, when an identity change is attempted while the team's current tournament is Ongoing.</exception>
    Task EnsureTeamIdentityEditableAsync(Team existingTeam, string? requestedName, string? requestedThreeLetterCode);

    Task UpdateTeamsAsync(IEnumerable<Team> teams);

    /// <summary>
    /// Deletes a team, guarding its competitive history.
    /// </summary>
    /// <param name="id">The unique identifier of the team to delete.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown, mapped to 409, when the team has match history, sanctions, point deductions, tournament registrations, or roster players with their own history.
    /// </exception>
    Task DeleteTeamAsync(Guid id);

    /// <summary>
    /// Retrieves a paginated list of teams based on filtering criteria.
    /// </summary>
    /// <param name="filter">The filtering criteria.</param>
    /// <returns>A paginated response containing the filtered teams.</returns>
    Task<PaginatedResponse<Team>> GetAllTeamsAsync(GetTeamsFilteredRequest filter);

    Task RegisterTeamsToTournamentAsync(Tournament tournament, List<Guid> teamIds);

    /// <summary>
    /// Enrolls a single team into tournament's registration phase, atomically.
    /// </summary>
    /// <param name="tournament">The tournament to enroll the team into. Must be OpenForRegistration.</param>
    /// <param name="existingTeamId">The existing team to enroll, or null.</param>
    /// <param name="newTeamName">The name of a brand-new team to create and enroll, or null.</param>
    /// <param name="copyRosterFromTournamentId">Optional source season whose roster to copy.</param>
    /// <returns>The enrolled team, with its roster scoped to this tournament.</returns>
    Task<Team> EnrollTeamAsync(
        Tournament tournament,
        Guid? existingTeamId,
        string? newTeamName,
        Guid? copyRosterFromTournamentId);

    /// <summary>
    /// Removes a team from a tournament, atomically.
    /// </summary>
    /// <param name="tournament">The tournament to remove the team from.</param>
    /// <param name="teamId">The id of the team to remove.</param>
    /// <exception cref="System.InvalidOperationException">The tournament is not in a phase that allows removing teams.</exception>
    /// <exception cref="System.Collections.Generic.KeyNotFoundException">The team is not enrolled in the tournament.</exception>
    Task UnenrollTeamAsync(Tournament tournament, Guid teamId);

    /// <summary>
    /// Builds the team's current group-stage standing row for a tournament, powering the public team-profile summary card.
    /// </summary>
    /// <param name="teamId">The team whose standing to locate.</param>
    /// <param name="tournamentId">
    /// The tournament to look in; when null the team has no season context and
    /// null is returned.
    /// </param>
    /// <returns>
    /// The team's standing row, or null when the team is in no group-stage
    /// standing for the tournament, whether playoff-only or with no finished matches.
    /// </returns>
    Task<TeamSummaryResponse?> GetTeamSummaryAsync(Guid teamId, Guid? tournamentId);

    /// <summary>
    /// Returns the team's matches, as home or visitor, in a tournament, oriented from the team's perspective and ordered by match date ascending.
    /// </summary>
    /// <param name="teamId">The team whose matches to list.</param>
    /// <param name="tournamentId">
    /// The tournament to scope the matches to; when null an empty list is
    /// returned.
    /// </param>
    /// <returns>The team's matches, oldest first; empty when there are none.</returns>
    Task<List<TeamMatchResponse>> GetTeamMatchesAsync(Guid teamId, Guid? tournamentId);

    /// <summary>
    /// Returns every tournament the team has participated in, enriched with season info, newest first.
    /// </summary>
    /// <param name="teamId">The team whose participation history to list.</param>
    /// <param name="currentTournamentId">
    /// The team's current tournament pointer, used to flag the current
    /// participation; when null no entry is flagged current.
    /// </param>
    /// <returns>The team's tournament participations.</returns>
    Task<List<TeamParticipationResponse>> GetTeamParticipationsAsync(Guid teamId, Guid? currentTournamentId);
}

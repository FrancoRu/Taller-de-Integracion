using Application.DTOs.Abstract.Response;
using Application.DTOs.Team.Request;

using Domain.Entities.Models;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces.Services;

/// <summary>
/// Represents a service for managing teams.
/// </summary>
public interface ITeamService
{
    /// <summary>
    /// Creates a new team.
    /// </summary>
    /// <param name="teamEntity">The team entity to create.</param>
    /// <returns>The created team.</returns>
    Task<Team> CreateTeamAsync(Team teamEntity);

    /// <summary>
    /// Retrieves a team by its id, with its roster (Players) scoped to one
    /// season — see <see cref="Domain.Entities.Models.PlayerTeamRegistration"/>.
    /// </summary>
    /// <param name="teamId">The id of the team to retrieve.</param>
    /// <param name="tournamentId">
    /// The season whose roster to attach. Defaults to the team's own current
    /// TournamentId when omitted, so callers get "today's roster" by default.
    /// A team with no current tournament (TournamentId null) and no explicit
    /// <paramref name="tournamentId"/> gets an empty roster.
    /// </param>
    /// <returns>The team with the specified id, or null if not found.</returns>
    Task<Team?> GetTeamByIdAsync(Guid teamId, Guid? tournamentId = null);

    /// <summary>
    /// Retrieves a team by its id or its slug, with its roster (Players)
    /// scoped to one season. The value is treated as an id when it parses as
    /// a GUID, otherwise it is looked up as a slug.
    /// </summary>
    /// <param name="idOrSlug">The team's GUID id or its slug.</param>
    /// <param name="tournamentId">
    /// The season whose roster to attach. Defaults to the team's own current
    /// TournamentId when omitted.
    /// </param>
    /// <returns>The matching team, or null if not found.</returns>
    Task<Team?> GetTeamByIdOrSlugAsync(string idOrSlug, Guid? tournamentId = null);

    /// <summary>
    /// Updates a team asynchronously.
    /// </summary>
    /// <param name="teamEntity">The team entity to update.</param>
    /// <returns>A boolean indicating whether the update was successful.</returns>
    Task UpdateTeamAsync(Team teamEntity);

    /// <summary>
    /// Updates multiple teams asynchronously.
    /// </summary>
    /// <param name="teams">The list of team entities to update.</param>
    /// <returns>A boolean indicating whether the update was successful.</returns>
    Task UpdateTeamsAsync(IEnumerable<Team> teams);

    Task DeleteTeamAsync(Guid id);

    /// <summary>
    /// Retrieves a paginated list of teams based on filtering criteria.
    /// </summary>
    /// <param name="filter">The filtering criteria.</param>
    /// <returns>A paginated response containing the filtered teams.</returns>
    Task<PaginatedResponse<Team>> GetAllTeamsAsync(GetTeamsFilteredRequest filter);

    Task RegisterTeamsToTournamentAsync(Tournament tournament, List<Guid> teamIds);

    /// <summary>
    /// HU-107: enrolls a single team into <paramref name="tournament"/>'s
    /// registration phase, atomically. Exactly one of
    /// <paramref name="existingTeamId"/> or <paramref name="newTeamName"/> must
    /// be supplied — the caller is expected to have validated that shape:
    /// <list type="bullet">
    /// <item><paramref name="newTeamName"/> creates a brand-new team with an
    /// empty roster and registers it.</item>
    /// <item><paramref name="existingTeamId"/> registers an existing team (a
    /// club from another season) — the same Team identity is reused (HU-99),
    /// only a new season registration is created.</item>
    /// </list>
    /// The enrollment is additive: teams already registered to this tournament
    /// are preserved. When <paramref name="copyRosterFromTournamentId"/> is set
    /// (only valid with <paramref name="existingTeamId"/>), that team's roster is
    /// cloned from the given season into this tournament as an editable base
    /// (HU-53); medical records are never inherited (HU-59).
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
    /// Removes a team from a tournament (HU-107 gap), atomically. Allowed only
    /// while the tournament is <see cref="Domain.Enums.TournamentStatus.OpenForRegistration"/>
    /// or <see cref="Domain.Enums.TournamentStatus.RegistrationClosed"/> — once
    /// the tournament has started the roster is frozen. Removes only THIS
    /// tournament's footprint for the team: its
    /// <see cref="TeamTournamentRegistration"/>, its
    /// <see cref="PlayerTeamRegistration"/> rows for this season, and its
    /// <see cref="StageTeamMatch"/> assignments in this tournament's stages;
    /// the team's registrations and roster in OTHER seasons are never touched.
    /// The denormalized <see cref="Team.TournamentId"/> pointer is cleared only
    /// when it currently points at this tournament.
    /// </summary>
    /// <param name="tournament">The tournament to remove the team from.</param>
    /// <param name="teamId">The id of the team to remove.</param>
    /// <exception cref="System.InvalidOperationException">The tournament is not in a phase that allows removing teams.</exception>
    /// <exception cref="System.Collections.Generic.KeyNotFoundException">The team is not enrolled in the tournament.</exception>
    Task UnenrollTeamAsync(Tournament tournament, Guid teamId);
}

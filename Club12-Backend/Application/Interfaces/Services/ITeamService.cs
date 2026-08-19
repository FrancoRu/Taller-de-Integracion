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
}

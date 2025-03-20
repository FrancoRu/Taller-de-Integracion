using Entities.DTOs.Abstract;
using Entities.DTOs.Team;
using Entities.Models.TeamEntity;

namespace Services.Services.TeamService;

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
    /// Retrieves a team by its id.
    /// </summary>
    /// <param name="teamId">The id of the team to retrieve.</param>
    /// <returns>The team with the specified id, or null if not found.</returns>
    Task<Team?> GetTeamByIdAsync(Guid teamId);

    /// <summary>
    /// Updates a team asynchronously.
    /// </summary>
    /// <param name="teamEntity">The team entity to update.</param>
    /// <returns>A boolean indicating whether the update was successful.</returns>
    Task<bool> UpdateTeamAsync(Team teamEntity);

    /// <summary>
    /// Updates multiple teams asynchronously.
    /// </summary>
    /// <param name="teams">The list of team entities to update.</param>
    /// <returns>A boolean indicating whether the update was successful.</returns>
    Task<bool> UpdateTeamsAsync(IEnumerable<Team> teams);

    /// <summary>
    /// Deletes a team asynchronously.
    /// </summary>
    /// <param name="teamEntity">The team to delete.</param>
    /// <returns>A boolean indicating whether the deletion was successful.</returns>
    Task<bool> DeleteTeamAsync(Team teamEntity);

    /// <summary>
    /// Retrieves a paginated list of teams based on filtering criteria.
    /// </summary>
    /// <param name="filter">The filtering criteria.</param>
    /// <returns>A paginated response containing the filtered teams.</returns>
    Task<PaginatedResponse<Team>> GetAllTeamsAsync(GetTeamsFilteredRequest filter);
}

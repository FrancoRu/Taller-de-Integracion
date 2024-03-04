using Club12.Entities.TeamEntity;

namespace Club12.Services.Teams;

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
    Team CreateTeam(Team teamEntity);

    /// <summary>
    /// Retrieves a team by its id.
    /// </summary>
    /// <param name="teamId">The id of the team to retrieve.</param>
    /// <returns>The team with the specified id, or null if not found.</returns>
    Team? GetTeamById(Guid teamId);

    /// <summary>
    /// Updates a team asynchronously.
    /// </summary>
    /// <param name="team">The team to update.</param>
    /// <returns>A boolean indicating whether the update was successful.</returns>
    Task<bool> UpdateTeam(Team team);

    /// <summary>
    /// Deletes a team.
    /// </summary>
    /// <param name="team">The team to delete.</param>
    void DeleteTeam(Team team);
}

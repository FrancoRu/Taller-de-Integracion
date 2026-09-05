using Domain.Entities.Models;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces.Services;

/// <summary>
/// Manages a team's technical staff, cuerpo técnico, DT and Asistente, scoped per team and tournament.
/// </summary>
public interface ITeamStaffService
{
    /// <summary>
    /// Creates a new staff member.
    /// </summary>
    /// <param name="staff">The staff member to create.</param>
    /// <returns>The created staff member, with the team navigation loaded.</returns>
    Task<TeamStaff> CreateAsync(TeamStaff staff);

    /// <summary>
    /// Returns every staff member for a team within a tournament, ordered by DateCreated ascending.
    /// </summary>
    /// <param name="teamId">The team whose staff to list.</param>
    /// <param name="tournamentId">The tournament, season, to scope the staff to.</param>
    Task<List<TeamStaff>> GetByTeamAndTournamentAsync(Guid teamId, Guid tournamentId);

    /// <summary>
    /// Deletes a staff member by their id. No-op when it does not exist.
    /// </summary>
    /// <param name="id">The id of the staff member to remove.</param>
    Task DeleteAsync(Guid id);
}

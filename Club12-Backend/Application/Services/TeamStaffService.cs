using Application.Interfaces.Repositories;
using Application.Interfaces.Services;

using Domain.Entities.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Services;

/// <summary>
/// Service managing a team's technical staff for a given tournament and season.
/// </summary>
public class TeamStaffService(
    ITeamStaffRepository staffRepository) : ITeamStaffService
{
    /// <inheritdoc/>
    public async Task<TeamStaff> CreateAsync(TeamStaff staff)
    {
        await staffRepository.AddAsync(staff);

        // Reload with the team navigation so the response can name the team.
        return await staffRepository.GetByIdAsync(
            staff.Id,
            includes: [entity => entity.Team!]) ?? staff;
    }

    /// <inheritdoc/>
    public async Task<List<TeamStaff>> GetByTeamAndTournamentAsync(Guid teamId, Guid tournamentId)
    {
        IEnumerable<TeamStaff> staff = await staffRepository.FindAsync(
            member => member.TeamId == teamId && member.TournamentId == tournamentId,
            includes: [member => member.Team!]);

        return [.. staff.OrderBy(member => member.DateCreated)];
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(Guid id)
    {
        await staffRepository.RemoveAsync(member => member.Id == id);
    }
}

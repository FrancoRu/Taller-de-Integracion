using Domain.Entities.Models;

namespace Application.Interfaces.Repositories;

/// <summary>
/// Repository interface for managing TeamStaff entities, a team's technical staff for a given tournament and season.
/// </summary>
public interface ITeamStaffRepository : IGenericRepository<TeamStaff>
{
}

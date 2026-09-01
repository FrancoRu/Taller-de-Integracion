using Domain.Entities.Models;

namespace Application.Interfaces.Repositories;

/// <summary>
/// Repository interface for managing <see cref="TeamStaff"/> entities (a
/// team's technical staff — cuerpo técnico — for a given tournament/season).
/// </summary>
public interface ITeamStaffRepository : IGenericRepository<TeamStaff>
{
}

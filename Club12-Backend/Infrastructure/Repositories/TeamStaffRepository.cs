using Application.Interfaces.Repositories;

using Domain.Entities.Models;

using Infrastructure.Persistance;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for TeamStaff entities, inheriting generic CRUD from GenericRepository.
/// </summary>
public class TeamStaffRepository(ApplicationDBContext context)
    : GenericRepository<TeamStaff>(context), ITeamStaffRepository
{
}

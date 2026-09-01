using Application.Interfaces.Repositories;

using Domain.Entities.Models;

using Infrastructure.Persistance;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for <see cref="TeamStaff"/> entities.
/// Inherits generic CRUD from GenericRepository{TeamStaff}.
/// </summary>
public class TeamStaffRepository(ApplicationDBContext context)
    : GenericRepository<TeamStaff>(context), ITeamStaffRepository
{
}

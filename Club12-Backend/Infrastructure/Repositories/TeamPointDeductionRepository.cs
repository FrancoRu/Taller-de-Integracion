using Application.Interfaces.Repositories;

using Domain.Entities.Models;

using Infrastructure.Persistance;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for TeamPointDeduction entities, inheriting generic CRUD from GenericRepository.
/// </summary>
public class TeamPointDeductionRepository(ApplicationDBContext context)
    : GenericRepository<TeamPointDeduction>(context), ITeamPointDeductionRepository
{
}

using Application.Interfaces.Repositories;

using Domain.Entities.Models;

using Infrastructure.Persistance;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for <see cref="TeamPointDeduction"/> entities.
/// Inherits generic CRUD from GenericRepository{TeamPointDeduction}.
/// </summary>
public class TeamPointDeductionRepository(ApplicationDBContext context)
    : GenericRepository<TeamPointDeduction>(context), ITeamPointDeductionRepository
{
}

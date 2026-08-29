using Domain.Entities.Models;

namespace Application.Interfaces.Repositories;

/// <summary>
/// Repository interface for managing <see cref="TeamPointDeduction"/> entities
/// (disciplinary point deductions applied to teams in a division).
/// </summary>
public interface ITeamPointDeductionRepository : IGenericRepository<TeamPointDeduction>
{
}

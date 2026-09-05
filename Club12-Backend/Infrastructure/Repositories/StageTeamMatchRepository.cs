

using Application.Interfaces.Repositories;

using Domain.Entities.Models;

using Infrastructure.Persistance;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for managing StageTeamMatch entities, inheriting generic CRUD from GenericRepository and implementing IStageTeamMatchRepository.
/// </summary>
public class StageTeamMatchRepository(ApplicationDBContext context)
    : GenericRepository<StageTeamMatch>(context), IStageTeamMatchRepository
{ }

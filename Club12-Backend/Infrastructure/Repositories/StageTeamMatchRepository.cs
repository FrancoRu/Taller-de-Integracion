

using Application.Interfaces.Repositories;

using Domain.Entities.Models;

using Infrastructure.Persistance;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for managing StageTeamMatch entities.
/// Inherits generic CRUD operations from GenericRepository{StageTeamMatch} and implements
/// IStageTeamMatchRepository for domain-specific data access.
/// </summary>
/// <remarks>
/// Uses ApplicationDBContext for database operations.
/// This repository is typically used by the service layer to interact with stage team match data.
/// </remarks>
public class StageTeamMatchRepository(ApplicationDBContext context)
    : GenericRepository<StageTeamMatch>(context), IStageTeamMatchRepository
{ }

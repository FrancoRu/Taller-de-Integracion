

using Application.Interfaces.Repositories;
using Domain.Entities.Models;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for managing <see cref="StageTeamMatch"/> entities.
/// Inherits generic CRUD operations from <see cref="GenericRepository{StageTeamMatch}"/> and implements
/// <see cref="IStageTeamMatchRepository"/> for domain-specific data access.
/// </summary>
/// <remarks>
/// Uses <see cref="ApplicationDBContext"/> for database operations.
/// This repository is typically used by the service layer to interact with stage team match data.
/// </remarks>
public class StageTeamMatchRepository(ApplicationDBContext context)
    : GenericRepository<StageTeamMatch>(context), IStageTeamMatchRepository
{ }

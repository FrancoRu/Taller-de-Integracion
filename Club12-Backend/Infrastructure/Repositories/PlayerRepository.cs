using Application.Interfaces.Repositories;

using Domain.Entities.Models;

using Infrastructure.Persistance;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for managing Player entities.
/// Inherits generic CRUD operations from GenericRepository{Player} and implements IPlayerRepository.
/// </summary>
/// <remarks>
/// Utilizes ApplicationDBContext for data access.
/// This repository is intended to encapsulate player-specific data operations.
/// </remarks>
public class PlayerRepository(ApplicationDBContext context)
    : GenericRepository<Player>(context), IPlayerRepository
{
}
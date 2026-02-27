using Application.Interfaces.Repositories;
using Domain.Entities.Models;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for managing <see cref="Player"/> entities.
/// Inherits generic CRUD operations from <see cref="GenericRepository{Player}"/> and implements <see cref="IPlayerRepository"/>.
/// </summary>
/// <remarks>
/// Utilizes <see cref="ApplicationDBContext"/> for data access.
/// This repository is intended to encapsulate player-specific data operations.
/// </remarks>
public class PlayerRepository(ApplicationDBContext context)
    : GenericRepository<Player>(context), IPlayerRepository
{
}
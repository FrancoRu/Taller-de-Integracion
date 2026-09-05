using Application.Interfaces.Repositories;

using Domain.Entities.Models;

using Infrastructure.Persistance;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for managing Player entities, inheriting generic CRUD and implementing IPlayerRepository.
/// </summary>
public class PlayerRepository(ApplicationDBContext context)
    : GenericRepository<Player>(context), IPlayerRepository
{
}
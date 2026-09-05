using Application.Interfaces.Repositories;

using Domain.Entities.Models;

using Infrastructure.Persistance;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for managing PlayerSanction entities, inheriting generic CRUD and implementing IPlayerSanctionRepository.
/// </summary>
public class PlayerSanctionRepository(ApplicationDBContext context)
    : GenericRepository<PlayerSanction>(context), IPlayerSanctionRepository
{
}
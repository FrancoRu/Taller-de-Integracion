using Application.Interfaces.Repositories;

using Domain.Entities.Models;

using Infrastructure.Persistance;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for managing PlayerStatistic entities, inheriting generic CRUD and implementing IPlayerStatisticRepository.
/// </summary>
public class PlayerStatisticRepository(ApplicationDBContext context)
    : GenericRepository<PlayerStatistic>(context), IPlayerStatisticRepository
{
}
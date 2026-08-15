using Application.Interfaces.Repositories;
using Domain.Entities.Models;
using Infrastructure.Persistance;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for managing PlayerStatistic entities.
/// Inherits generic CRUD operations from GenericRepository{PlayerStatistic} and implements IPlayerStatisticRepository.
/// Utilizes ApplicationDBContext for data access.
/// </summary>
public class PlayerStatisticRepository(ApplicationDBContext context)
    : GenericRepository<PlayerStatistic>(context), IPlayerStatisticRepository
{
}
using Application.Interfaces.Repositories;
using Domain.Entities.Models;
using Infrastructure.Persistance;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for managing <see cref="PlayerStatistic"/> entities.
/// Inherits generic CRUD operations from <see cref="GenericRepository{PlayerStatistic}"/> and implements <see cref="IPlayerStatisticRepository"/>.
/// Utilizes <see cref="ApplicationDBContext"/> for data access.
/// </summary>
public class PlayerStatisticRepository(ApplicationDBContext context)
    : GenericRepository<PlayerStatistic>(context), IPlayerStatisticRepository
{
}
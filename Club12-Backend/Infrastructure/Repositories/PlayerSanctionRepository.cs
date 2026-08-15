using Application.Interfaces.Repositories;
using Domain.Entities.Models;
using Infrastructure.Persistance;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for managing PlayerSanction entities.
/// Inherits generic CRUD operations from GenericRepository{PlayerSanction} and implements IPlayerSanctionRepository.
/// Utilizes ApplicationDBContext for data access.
/// </summary>
public class PlayerSanctionRepository(ApplicationDBContext context)
    : GenericRepository<PlayerSanction>(context), IPlayerSanctionRepository
{
}
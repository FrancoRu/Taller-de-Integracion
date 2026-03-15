using Application.Interfaces.Repositories;
using Domain.Entities.Models;
using Infrastructure.Persistance;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for managing <see cref="PlayerSanction"/> entities.
/// Inherits generic CRUD operations from <see cref="GenericRepository{PlayerSanction}"/> and implements <see cref="IPlayerSanctionRepository"/>.
/// Utilizes <see cref="ApplicationDBContext"/> for data access.
/// </summary>
public class PlayerSanctionRepository(ApplicationDBContext context)
    : GenericRepository<PlayerSanction>(context), IPlayerSanctionRepository
{
}
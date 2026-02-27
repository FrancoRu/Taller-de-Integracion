using Application.Interfaces.Repositories;
using Domain.Entities.Models;

namespace Infrastructure.Repositories;

public class PlayerSanctionRepository(ApplicationDBContext context) 
    : GenericRepository<PlayerSanction>(context), IPlayerSanctionRepository
{
}

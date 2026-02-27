using Application.Interfaces.Repositories;
using Domain.Entities.Models;

namespace Infrastructure.Repositories;

public class PlayerRepository(ApplicationDBContext context) 
    : GenericRepository<Player>(context), IPlayerRepository
{
}

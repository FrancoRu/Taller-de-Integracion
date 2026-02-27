using Application.Interfaces.Repositories;
using Domain.Entities.Models;

namespace Infrastructure.Repositories;

public class PlayerStatisticRepository(ApplicationDBContext context) 
    : GenericRepository<PlayerStatistic>(context), IPlayerStatisticRepository
{
}

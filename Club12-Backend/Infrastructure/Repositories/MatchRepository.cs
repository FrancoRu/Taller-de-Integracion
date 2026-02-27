using Application.Interfaces.Repositories;
using Domain.Entities.Models;

namespace Infrastructure.Repositories;

public class MatchRepository(ApplicationDBContext context) 
    : GenericRepository<Match>(context), IMatchRepository
{
}

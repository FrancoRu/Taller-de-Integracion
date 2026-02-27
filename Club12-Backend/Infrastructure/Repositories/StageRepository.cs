using Application.Interfaces.Repositories;
using Domain.Entities.Models;

namespace Infrastructure.Repositories;

public class StageRepository(ApplicationDBContext context) 
    : GenericRepository<Stage>(context), IStageRepository
{
}

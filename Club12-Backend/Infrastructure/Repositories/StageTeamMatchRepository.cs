

using Application.Interfaces.Repositories;
using Domain.Entities.Models;

namespace Infrastructure.Repositories;

public class StageTeamMatchRepository(ApplicationDBContext context) 
    : GenericRepository<StageTeamMatch>(context), IStageTeamMatchRepository { }

using Application.Interfaces.Repositories;
using Domain.Entities.Models;

namespace Infrastructure.Repositories;

public class TeamRepository(ApplicationDBContext context) 
    : GenericRepository<Team>(context), ITeamRepository {}

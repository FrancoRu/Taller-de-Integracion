using Application.Interfaces.Repositories;
using Domain.Entities.Models;

namespace Infrastructure.Repositories;

public class TournamentRepository(ApplicationDBContext context) 
    : GenericRepository<Tournament>(context), ITournamentRepository {}

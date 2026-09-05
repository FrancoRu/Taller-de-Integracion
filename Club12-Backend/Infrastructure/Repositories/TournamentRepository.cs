using Application.Interfaces.Repositories;

using Domain.Entities.Models;

using Infrastructure.Persistance;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for managing Tournament entities, inheriting generic CRUD from GenericRepository and implementing ITournamentRepository.
/// </summary>
public class TournamentRepository(ApplicationDBContext context)
    : GenericRepository<Tournament>(context), ITournamentRepository
{ }
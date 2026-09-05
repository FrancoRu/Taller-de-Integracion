using Application.Interfaces.Repositories;

using Domain.Entities.Models;

using Infrastructure.Persistance;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for managing Team entities, inheriting generic CRUD from GenericRepository and implementing ITeamRepository.
/// </summary>
public class TeamRepository(ApplicationDBContext context)
    : GenericRepository<Team>(context), ITeamRepository
{ }

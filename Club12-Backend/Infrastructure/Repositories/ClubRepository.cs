using Application.Interfaces.Repositories;

using Domain.Entities.Models;

using Infrastructure.Persistance;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for managing Club entities, inheriting generic CRUD from GenericRepository.
/// </summary>
public class ClubRepository(ApplicationDBContext context)
    : GenericRepository<Club>(context), IClubRepository
{ }

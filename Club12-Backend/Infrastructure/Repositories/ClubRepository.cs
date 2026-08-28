using Application.Interfaces.Repositories;

using Domain.Entities.Models;

using Infrastructure.Persistance;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for managing Club entities (HU-99).
/// Inherits generic CRUD operations from GenericRepository{Club}.
/// </summary>
public class ClubRepository(ApplicationDBContext context)
    : GenericRepository<Club>(context), IClubRepository
{ }

using Application.Interfaces.Repositories;
using Domain.Entities.Models;
using Infrastructure.Persistance;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for managing Tournament entities.
/// Inherits generic CRUD operations from GenericRepository{Tournament} and
/// implements ITournamentRepository for tournament-specific data access.
/// </summary>
/// <remarks>
/// Uses ApplicationDBContext for database operations.
/// This repository is intended to encapsulate tournament-related persistence logic.
/// </remarks>
public class TournamentRepository(ApplicationDBContext context)
    : GenericRepository<Tournament>(context), ITournamentRepository
{ }
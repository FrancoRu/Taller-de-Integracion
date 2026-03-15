using Application.Interfaces.Repositories;
using Domain.Entities.Models;
using Infrastructure.Persistance;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for managing <see cref="Tournament"/> entities.
/// Inherits generic CRUD operations from <see cref="GenericRepository{Tournament}"/> and
/// implements <see cref="ITournamentRepository"/> for tournament-specific data access.
/// </summary>
/// <remarks>
/// Uses <see cref="ApplicationDBContext"/> for database operations.
/// This repository is intended to encapsulate tournament-related persistence logic.
/// </remarks>
public class TournamentRepository(ApplicationDBContext context)
    : GenericRepository<Tournament>(context), ITournamentRepository
{ }
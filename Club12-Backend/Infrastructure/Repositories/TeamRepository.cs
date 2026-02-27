using Application.Interfaces.Repositories;
using Domain.Entities.Models;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for managing <see cref="Team"/> entities.
/// Inherits generic CRUD operations from <see cref="GenericRepository{Team}"/> and implements <see cref="ITeamRepository"/> interface.
/// Utilizes <see cref="ApplicationDBContext"/> for data access.
/// </summary>
public class TeamRepository(ApplicationDBContext context) 
    : GenericRepository<Team>(context), ITeamRepository {}

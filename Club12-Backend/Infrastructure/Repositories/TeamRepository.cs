using Application.Interfaces.Repositories;
using Domain.Entities.Models;
using Infrastructure.Persistance;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for managing Team entities.
/// Inherits generic CRUD operations from GenericRepository{Team} and implements ITeamRepository interface.
/// Utilizes ApplicationDBContext for data access.
/// </summary>
public class TeamRepository(ApplicationDBContext context) 
    : GenericRepository<Team>(context), ITeamRepository {}

using Application.Interfaces.Repositories;

using Domain.Entities.Models;

using Infrastructure.Persistance;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for managing PlayerTeamRegistration entities.
/// Inherits generic CRUD operations from GenericRepository{PlayerTeamRegistration}.
/// </summary>
public class PlayerTeamRegistrationRepository(ApplicationDBContext context)
    : GenericRepository<PlayerTeamRegistration>(context), IPlayerTeamRegistrationRepository
{ }

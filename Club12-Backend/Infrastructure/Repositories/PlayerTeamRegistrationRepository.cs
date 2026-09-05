using Application.Interfaces.Repositories;

using Domain.Entities.Models;

using Infrastructure.Persistance;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for managing PlayerTeamRegistration entities, inheriting generic CRUD from GenericRepository.
/// </summary>
public class PlayerTeamRegistrationRepository(ApplicationDBContext context)
    : GenericRepository<PlayerTeamRegistration>(context), IPlayerTeamRegistrationRepository
{ }

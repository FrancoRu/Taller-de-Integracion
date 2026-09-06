using Application.Interfaces.Repositories;

using Domain.Entities.Models;

using Infrastructure.Persistance;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for managing DivisionTeamRegistration entities, inheriting generic CRUD from GenericRepository.
/// </summary>
public class DivisionTeamRegistrationRepository(ApplicationDBContext context)
    : GenericRepository<DivisionTeamRegistration>(context), IDivisionTeamRegistrationRepository
{ }

using Application.Interfaces.Repositories;

using Domain.Entities.Models;

using Infrastructure.Persistance;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for managing TeamTournamentRegistration entities, inheriting generic CRUD from GenericRepository.
/// </summary>
public class TeamTournamentRegistrationRepository(ApplicationDBContext context)
    : GenericRepository<TeamTournamentRegistration>(context), ITeamTournamentRegistrationRepository
{ }

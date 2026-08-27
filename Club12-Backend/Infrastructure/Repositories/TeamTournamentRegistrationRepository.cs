using Application.Interfaces.Repositories;

using Domain.Entities.Models;

using Infrastructure.Persistance;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for managing TeamTournamentRegistration entities.
/// Inherits generic CRUD operations from GenericRepository{TeamTournamentRegistration}.
/// </summary>
public class TeamTournamentRegistrationRepository(ApplicationDBContext context)
    : GenericRepository<TeamTournamentRegistration>(context), ITeamTournamentRegistrationRepository
{ }

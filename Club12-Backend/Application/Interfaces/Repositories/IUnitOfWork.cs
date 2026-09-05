using System;
using System.Threading.Tasks;

namespace Application.Interfaces.Repositories;

/// <summary>
/// Unit of Work interface for coordinating multiple repositories and saving changes atomically.
/// </summary>
public interface IUnitOfWork
{
    IBlogPostRepository BlogPostRepository { get; }
    IClubRepository ClubRepository { get; }
    ITeamRepository TeamRepository { get; }
    IVenueRepository VenueRepository { get; }
    ITournamentRepository TournamentRepository { get; }
    IStageRepository StageRepository { get; }
    IPlayerStatisticRepository PlayerStatisticRepository { get; }
    IPlayerSanctionRepository PlayerSanctionRepository { get; }
    IPlayerRepository PlayerRepository { get; }
    IMatchRepository MatchRepository { get; }
    IMatchSeriesRepository MatchSeriesRepository { get; }
    IDivisionRepository DivisionRepository { get; }
    IStageTeamMatchRepository StageTeamMatchRepository { get; }
    IPlayerTeamRegistrationRepository PlayerTeamRegistrationRepository { get; }
    ITeamTournamentRegistrationRepository TeamTournamentRegistrationRepository { get; }
    Task<int> SaveChangesAsync();

    /// <summary>
    /// Runs operation inside a single database transaction, rolling back entirely with no partial writes if the operation throws.
    /// </summary>
    Task ExecuteInTransactionAsync(Func<Task> operation);
}

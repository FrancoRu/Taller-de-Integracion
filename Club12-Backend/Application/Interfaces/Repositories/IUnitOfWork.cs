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
    /// Runs <paramref name="operation"/> inside a single database transaction
    /// (HU-38). Every repository shares this unit of work's DbContext, so any
    /// nested SaveChanges the operation triggers participates in — and only
    /// commits with — this transaction. If the operation throws, the whole
    /// transaction is rolled back, leaving no partial writes. Wrapped in the
    /// context's execution strategy so it stays compatible with retrying
    /// providers.
    /// </summary>
    Task ExecuteInTransactionAsync(Func<Task> operation);
}

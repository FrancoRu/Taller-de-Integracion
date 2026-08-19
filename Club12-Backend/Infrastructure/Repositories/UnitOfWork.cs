using Application.Interfaces.Repositories;

using Infrastructure.Persistance;

using System.Threading.Tasks;

namespace Infrastructure.Repositories;

/// <summary>
/// Implements the IUnitOfWork interface, providing a centralized access point for all repository instances
/// and managing transactional operations within the application's data layer.
/// </summary>
/// <remarks>
/// This class uses constructor injection to receive all repository dependencies and the DbContext instance.
/// It exposes each repository via a public property, enabling coordinated data operations across multiple entities.
/// The SaveChangesAsync method commits all changes to the database in a single transaction.
/// </remarks>
public class UnitOfWork(
    ApplicationDBContext context,
    IPlayerRepository playerRepository,
    ITeamRepository teamRepository,
    IDivisionRepository divisionRepository,
    IMatchRepository matchRepository,
    IMatchSeriesRepository matchSeriesRepository,
    ITournamentRepository tournamentRepository,
    IBlogPostRepository blogPostRepository,
    IPlayerSanctionRepository playerSanctionRepository,
    IPlayerStatisticRepository playerStatisticRepository,
    IVenueRepository venueRepository,
    IStageRepository stageRepository,
    IStageTeamMatchRepository stageTeamMatchRepository,
    IPlayerTeamRegistrationRepository playerTeamRegistrationRepository
    ) : IUnitOfWork
{
    /// <summary>
    /// Gets the repository for blog post entities.
    /// </summary>
    public IBlogPostRepository BlogPostRepository { get; } = blogPostRepository;

    /// <summary>
    /// Gets the repository for team entities.
    /// </summary>
    public ITeamRepository TeamRepository { get; } = teamRepository;

    /// <summary>
    /// Gets the repository for venue entities.
    /// </summary>
    public IVenueRepository VenueRepository { get; } = venueRepository;

    /// <summary>
    /// Gets the repository for tournament entities.
    /// </summary>
    public ITournamentRepository TournamentRepository { get; } = tournamentRepository;

    /// <summary>
    /// Gets the repository for stage entities.
    /// </summary>
    public IStageRepository StageRepository { get; } = stageRepository;


    /// <summary>
    /// Gets the repository for player statistic entities.
    /// </summary>
    public IPlayerStatisticRepository PlayerStatisticRepository { get; } = playerStatisticRepository;

    /// <summary>
    /// Gets the repository for player sanction entities.
    /// </summary>
    public IPlayerSanctionRepository PlayerSanctionRepository { get; } = playerSanctionRepository;

    /// <summary>
    /// Gets the repository for player entities.
    /// </summary>
    public IPlayerRepository PlayerRepository { get; } = playerRepository;

    /// <summary>
    /// Gets the repository for match entities.
    /// </summary>
    public IMatchRepository MatchRepository { get; } = matchRepository;

    /// <summary>
    /// Gets the repository for match series entities.
    /// </summary>
    public IMatchSeriesRepository MatchSeriesRepository { get; } = matchSeriesRepository;

    /// <summary>
    /// Gets the repository for division entities.
    /// </summary>
    public IDivisionRepository DivisionRepository { get; } = divisionRepository;

    /// <summary>
    /// Gets the repository for stage team match entities.
    /// </summary>
    public IStageTeamMatchRepository StageTeamMatchRepository { get; } = stageTeamMatchRepository;

    /// <summary>
    /// Gets the repository for player-team registration entities.
    /// </summary>
    public IPlayerTeamRegistrationRepository PlayerTeamRegistrationRepository { get; } = playerTeamRegistrationRepository;

    /// <summary>
    /// Commits all tracked changes to the database asynchronously.
    /// </summary>
    /// <returns>The number of state entries written to the database.</returns>
    public async Task<int> SaveChangesAsync()
    {
        return await context.SaveChangesAsync();
    }
}

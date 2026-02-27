using Application.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Infrastructure.Repositories;

public class UnitOfWork(
    DbContext context,
    IPlayerRepository playerRepository,
    ITeamRepository teamRepository,
    IDivisionRepository divisionRepository,
    IMatchRepository matchRepository,
    ITournamentRepository tournamentRepository,
    IBlogPostRepository blogPostRepository,
    IStaffRepository staffRepository,
    IPlayerSanctionRepository playerSanctionRepository,
    IPlayerStatisticRepository playerStatisticRepository,
    IVenueRepository venueRepository,
    IStageRepository stageRepository,
    IStageTeamMatchRepository stageTeamMatchRepository
    ) : IUnitOfWork
{
    public IBlogPostRepository BlogPostRepository { get; } = blogPostRepository;
    public ITeamRepository TeamRepository { get; } = teamRepository;
    public IVenueRepository VenueRepository { get; } = venueRepository;
    public ITournamentRepository TournamentRepository { get; } = tournamentRepository;
    public IStageRepository StageRepository { get; } = stageRepository;
    public IStaffRepository StaffRepository { get; } = staffRepository;
    public IPlayerStatisticRepository PlayerStatisticRepository { get; } = playerStatisticRepository;
    public IPlayerSanctionRepository PlayerSanctionRepository { get; } = playerSanctionRepository;
    public IPlayerRepository PlayerRepository { get; } = playerRepository;
    public IMatchRepository MatchRepository { get; } = matchRepository;
    public IDivisionRepository DivisionRepository { get; } = divisionRepository;
    public IStageTeamMatchRepository StageTeamMatchRepository { get; } = stageTeamMatchRepository;
    public async Task<int> SaveChangesAsync() 
        => await context.SaveChangesAsync();
}

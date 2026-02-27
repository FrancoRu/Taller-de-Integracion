using Domain.Entities.Models;
using System.Threading.Tasks;

namespace Application.Interfaces.Repositories;

/// <summary>
/// Unit of Work interface for coordinating multiple repositories and saving changes atomically.
/// </summary>
public interface IUnitOfWork
{
    IBlogPostRepository BlogPostRepository { get; }
    ITeamRepository TeamRepository { get; }
    IVenueRepository VenueRepository { get; }
    ITournamentRepository TournamentRepository { get; }
    IStageRepository StageRepository { get; }
    IStaffRepository StaffRepository { get; }
    IPlayerStatisticRepository PlayerStatisticRepository { get; }
    IPlayerSanctionRepository PlayerSanctionRepository { get; }
    IPlayerRepository PlayerRepository { get; }
    IMatchRepository MatchRepository { get; }
    IDivisionRepository DivisionRepository { get; }
    IStageTeamMatchRepository StageTeamMatchRepository { get; }
    Task<int> SaveChangesAsync();
}

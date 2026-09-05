using Domain.Entities.Models;

using Microsoft.EntityFrameworkCore;


namespace Infrastructure.Persistance;

/// <summary>
/// Defines the database context interface for the Club12 application.
/// Provides access to the various DbSets representing entities in the system.
/// </summary>
public interface IClub12DBContext
{
    DbSet<BackupRecord> BackupRecords { get; }

    DbSet<Team> Teams { get; }

    DbSet<Player> Players { get; }

    DbSet<Tournament> Tournaments { get; }

    DbSet<Division> Divisions { get; }

    DbSet<Match> Matches { get; }

    /// <summary>
    /// Best-of-N playoff series; each series groups the individual <see cref="Match"/>
    /// rows played between the same two teams.
    /// </summary>
    DbSet<MatchSeries> MatchSeries { get; }

    DbSet<PlayerStatistic> PlayersStatistics { get; }

    /// <summary>
    /// A sanction's subject may be a player, a team, or a staff member (see
    /// <see cref="PlayerSanction.SubjectType"/>), despite the entity's name.
    /// </summary>
    DbSet<PlayerSanction> PlayerSanctions { get; }

    DbSet<Venue> Venues { get; }

    DbSet<BlogPost> BlogPosts { get; }

    /// <summary>
    /// Seasons ("Temporadas") group tournaments.
    /// </summary>
    DbSet<Season> Seasons { get; }

    DbSet<Stage> Stages { get; }

    DbSet<StageTeamMatch> StageTeamMatches { get; }
}

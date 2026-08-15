using Domain.Entities.Models;
using Microsoft.EntityFrameworkCore;


namespace Infrastructure.Persistance;

/// <summary>
/// Defines the database context interface for the Club12 application.
/// Provides access to the various DbSets representing entities in the system.
/// </summary>
public interface IClub12DBContext
{
    /// <summary>
    /// DbSet{Team} of teams in the system.
    /// </summary>
    DbSet<Team> Teams { get; }

    /// <summary>
    /// DbSet{Player} of players in the system.
    /// </summary>
    DbSet<Player> Players { get; }

    /// <summary>
    /// DbSet{Tournament} of tournaments in the system.
    /// </summary>
    DbSet<Tournament> Tournaments { get; }

    /// <summary>
    /// DbSet{Division} of divisions in the system.
    /// </summary>
    DbSet<Division> Divisions { get; }

    /// <summary>
    /// DbSet{Match} of matches in the system.
    /// </summary>
    DbSet<Match> Matches { get; }

    /// <summary>
    /// DbSet{PlayerStatistic} of player statistics in the system.
    /// </summary>
    DbSet<PlayerStatistic> PlayersStatistics { get; }

    /// <summary>
    /// DbSet{PlayerSanction} of players involved in sanctions.
    /// </summary>
    DbSet<PlayerSanction> PlayerSanctions { get; }

    /// <summary>
    /// DbSet{Venue} of venues in the system.
    /// </summary>
    DbSet<Venue> Venues { get; }

    /// <summary>
    /// DbSet{BlogPosts} of blog posts in the system.
    /// </summary>
    DbSet<BlogPost> BlogPosts { get; }


    /// <summary>
    /// DbSet{Stage} of Stages in the system.
    /// </summary>
    DbSet<Stage> Stages { get; }

    /// <summary>
    /// DbSet{StageTeamMatch} of StageTeamMatches in the system.
    /// </summary>
    DbSet<StageTeamMatch> StageTeamMatches { get; }
}

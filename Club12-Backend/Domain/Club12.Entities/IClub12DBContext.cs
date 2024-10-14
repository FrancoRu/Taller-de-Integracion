using Club12.Entities.DivisionEntity;
using Club12.Entities.MatchEntity;
using Club12.Entities.PlayerEntity;
using Club12.Entities.PlayersStatisticEntity;
using Club12.Entities.SanctionPlayerEntity;
using Club12.Entities.TeamEntity;
using Club12.Entities.TournamentEntity;
using Club12.Entities.UserEntity;
using Microsoft.EntityFrameworkCore;

namespace Club12.Entities;

/// <summary>
/// Defines the database context interface for the Club12 application.
/// Provides access to the various DbSets representing entities in the system.
/// </summary>
public interface IClub12DBContext
{
    /// <summary>
    /// <see cref="DbSet{Team}"/> of teams in the system.
    /// </summary>
    DbSet<Team> Teams { get; }

    /// <summary>
    /// <see cref="DbSet{Player}"/> of players in the system.
    /// </summary>
    DbSet<Player> Players { get; }

    /// <summary>
    /// <see cref="DbSet{Tournament}"/> of tournaments in the system.
    /// </summary>
    DbSet<Tournament> Tournaments { get; }

    /// <summary>
    /// <see cref="DbSet{Division}"/> of divisions in the system.
    /// </summary>
    DbSet<Division> Divisions { get; }

    /// <summary>
    /// <see cref="DbSet{Match}"/> of matches in the system.
    /// </summary>
    DbSet<Match> Matches { get; }

    /// <summary>
    /// <see cref="DbSet{PlayerStatistic}"/> of player statistics in the system.
    /// </summary>
    DbSet<PlayerStatistic> PlayersStatistics { get; }

    /// <summary>
    /// <see cref="DbSet{SanctionPlayer}"/> of players involved in sanctions.
    /// </summary>
    DbSet<PlayerSanction> PlayerSanctions { get; }

    /// <summary>
    /// <see cref="DbSet{User}"/> of users in the system.
    /// </summary>
    DbSet<User> Users { get; set; }
}

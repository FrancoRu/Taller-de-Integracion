using Entities.Models.DivisionEntity;
using Entities.Models.MatchEntity;
using Entities.Models.PlayerEntity;
using Entities.Models.PlayerSanctionEntity;
using Entities.Models.PlayerStatisticEntity;
using Entities.Models.TeamEntity;
using Entities.Models.TournamentEntity;
using Entities.Models.UserEntity;
using Microsoft.EntityFrameworkCore;

namespace Entities;

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
    /// <see cref="DbSet{PlayerSanction}"/> of players involved in sanctions.
    /// </summary>
    DbSet<PlayerSanction> PlayerSanctions { get; }

    /// <summary>
    /// <see cref="DbSet{User}"/> of users in the system.
    /// </summary>
    DbSet<User> Users { get; set; }
}

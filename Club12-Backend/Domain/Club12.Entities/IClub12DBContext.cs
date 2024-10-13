using Club12.Entities.DivisionEntity;
using Club12.Entities.MatchEntity;
using Club12.Entities.PlayerEntity;
using Club12.Entities.PlayersStatisticEntity;
using Club12.Entities.SancitonEntity;
using Club12.Entities.SanctionPlayerEntity;
using Club12.Entities.StatisticEntity;
using Club12.Entities.TeamEntity;
using Club12.Entities.TournamentEntity;
using Club12.Entities.UserEntity;
using Microsoft.EntityFrameworkCore;

namespace Club12.Entities;

public interface IClub12DBContext
{
    DbSet<Team> Teams { get; }

    DbSet<Player> Players { get; }

    DbSet<Tournament> Tournaments { get; }

    DbSet<Division> Divisions { get; }

    DbSet<Match> Matches { get; }

    DbSet<PlayerStatistic> PlayersStatistics { get; }

    DbSet<Statistic> Statistics { get; }

    DbSet<Sanction> Sanctions { get; }

    DbSet<SanctionPlayer> SanctionsPlayers { get; }

    DbSet<User> Users { get; set; }
}

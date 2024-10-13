using Club12.Entities;
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

namespace Persistence;

/// <summary>
/// This is a placeholder interface that inherits from all domain DBContext interfaces.
/// </summary>
internal interface IDomainDBContexts : IClub12DBContext
{

}

public class ApplicationDBContext : DbContext, IDomainDBContexts
{

    public ApplicationDBContext(DbContextOptions<ApplicationDBContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Match>()
        .Property(match => match.Type)
        .HasConversion(
            value => value.ToString(),
            value => (MatchType)Enum.Parse(typeof(MatchType), value));

        modelBuilder.Entity<Team>()
            .HasMany(team => team.Players)
            .WithOne(player => player.Team)
            .HasForeignKey(player => player.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        base.OnModelCreating(modelBuilder);
    }

    public virtual DbSet<Team> Teams { get; set; }

    public virtual DbSet<Player> Players { get; set; }

    public virtual DbSet<Tournament> Tournaments { get; set; }

    public virtual DbSet<Division> Divisions { get; set; }

    public virtual DbSet<Match> Matches { get; set; }

    public virtual DbSet<PlayerStatistic> PlayersStatistics { get; set; }

    public virtual DbSet<Statistic> Statistics { get; set; }

    public virtual DbSet<Sanction> Sanctions { get; set; }

    public virtual DbSet<SanctionPlayer> SanctionsPlayers { get; set; }

    public virtual DbSet<User> Users { get; set; }
}
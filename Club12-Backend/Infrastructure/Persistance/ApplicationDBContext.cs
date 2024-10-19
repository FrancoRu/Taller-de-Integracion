using Entities;
using Entities.Models.DivisionEntity;
using Entities.Models.MatchEntity;
using Entities.Models.PlayerEntity;
using Entities.Models.PlayerSanctionEntity;
using Entities.Models.PlayerStatisticEntity;
using Entities.Models.TeamEntity;
using Entities.Models.TournamentEntity;
using Entities.Models.UserEntity;
using Microsoft.EntityFrameworkCore;

namespace Persistence;

/// <summary>
/// This is a placeholder interface that inherits from all domain DBContext interfaces.
/// </summary>
internal interface IDomainDBContexts : IClub12DBContext
{

}

public class ApplicationDBContext(DbContextOptions<ApplicationDBContext> options) : DbContext(options), IDomainDBContexts
{
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

    public virtual DbSet<PlayerSanction> PlayerSanctions { get; set; }

    public virtual DbSet<User> Users { get; set; }
}
using Entities;
using Entities.Models.BlogPosts;
using Entities.Models.Divisions;
using Entities.Models.Matches;
using Entities.Models.Players;
using Entities.Models.PlayerSanctions;
using Entities.Models.PlayerStatistics;
using Entities.Models.Staffs;
using Entities.Models.Stages;
using Entities.Models.Teams;
using Entities.Models.Tournaments;
using Entities.Models.Users;
using Entities.Models.Venues;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Persistance.Conventions;
using MatchType = Entities.Models.Matches.MatchType;

namespace Persistance;

/// <summary>
/// This is a placeholder interface that inherits from all domain DBContext interfaces.
/// </summary>
public interface IDomainDBContexts : IClub12DBContext
{

}


public class ApplicationDBContext(DbContextOptions<ApplicationDBContext> options) : DbContext(options), IDomainDBContexts
{
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Conventions.Add(_ => new DateTimeToTimestampWithoutTimeZoneConvention());

        base.ConfigureConventions(configurationBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Match>()
            .Property(match => match.Type)
            .HasConversion(
                value => value.ToString(),
                value => (MatchType) Enum.Parse(typeof(MatchType), value)
            );

        modelBuilder.Entity<Stage>()
            .Property(stage => stage.StageType)
            .HasConversion(
                value => value.ToString(),
                value => (StageType) Enum.Parse(typeof(StageType), value)
            );
        modelBuilder.Entity<Stage>()
            .HasIndex(s => new { s.Name, s.DivisionId })
            .IsUnique()
            .HasDatabaseName("CONSTRAINT_UNIQUE_STAGE_NAME_AND_DIVISIONID");

        modelBuilder.Entity<Staff>()
            .Property(staff => staff.Type)
            .HasConversion(
                value => value.ToString(),
                value => (StaffType) Enum.Parse(typeof(StaffType), value));

        modelBuilder.Entity<Team>()
            .HasMany(team => team.Players)
            .WithOne(player => player.Team)
            .HasForeignKey(player => player.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Division>()
            .HasMany(division => division.Stages)
            .WithOne(stage => stage.Division)
            .HasForeignKey(stages => stages.DivisionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Staff>()
            .Property(staffType => staffType.Type)
            .HasConversion(
                value => value.ToString(),
                value => (StaffType) Enum.Parse(typeof(StaffType), value)
            );

        modelBuilder.Entity<Tournament>()
            .ToTable(t => 
                t.HasCheckConstraint(
                    "CK_Tournament_DeadlineBeforeStart",
                    "\"TeamRegistrationDeadline\" < \"StartDate\""
                    )
                );

        base.OnModelCreating(modelBuilder);

    }

    public virtual required DbSet<Team> Teams { get; set; }

    public virtual required DbSet<Player> Players { get; set; }

    public virtual required DbSet<Tournament> Tournaments { get; set; }

    public virtual required DbSet<Division> Divisions { get; set; }

    public virtual required DbSet<Match> Matches { get; set; }

    public virtual required DbSet<PlayerStatistic> PlayersStatistics { get; set; }

    public virtual required DbSet<PlayerSanction> PlayerSanctions { get; set; }

    public virtual required DbSet<User> Users { get; set; }

    public virtual required DbSet<Venue> Venues { get; set; }

    public virtual required DbSet<BlogPost> BlogPosts { get; set; }

    public virtual required DbSet<Staff> Staffs { get; set; }

    public virtual required DbSet<Stage> Stages { get; set; }
}
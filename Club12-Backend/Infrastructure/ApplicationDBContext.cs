
using System;
using Domain;
using Domain.Entities.Models;
using Domain.Enums;
using Infrastructure.Conventions;
using Microsoft.EntityFrameworkCore;


namespace Infrastructure;

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
        modelBuilder.Entity<StageTeamMatch>()
            .HasOne(stm => stm.Stage)
            .WithMany(stage => stage.StageTeamMatches)
            .HasForeignKey(stm => stm.StageId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<StageTeamMatch>()
            .HasOne(stm => stm.Team)
            .WithMany(team => team.StageTeamMatches)
            .HasForeignKey(stm => stm.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

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

    public virtual required DbSet<StageTeamMatch> StageTeamMatches { get; set; }
}
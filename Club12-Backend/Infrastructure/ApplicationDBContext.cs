
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


/// <summary>
/// Represents the application's main Entity Framework Core database context, implementing domain-specific context interfaces.
/// </summary>
/// <remarks>
/// <para>
/// The <c>ApplicationDBContext</c> configures entity relationships, property conversions, and database constraints for the Club12 domain.
/// </para>
/// <list type="bullet">
/// <item>
/// <b>ConfigureConventions:</b> Adds custom conventions, such as converting <see cref="DateTime"/> properties to timestamp without time zone.
/// </item>
/// <item>
/// <b>OnModelCreating:</b> Configures entity mappings:
/// <ul>
///   <li>Enum properties (<c>MatchType</c>, <c>StageType</c>, <c>StaffType</c>) are stored as strings.</li>
///   <li>Unique index on <c>Stage</c> (<c>Name</c>, <c>DivisionId</c>).</li>
///   <li>Relationships with cascade delete for <c>Team</c>-<c>Player</c>, <c>Division</c>-<c>Stage</c>, <c>StageTeamMatch</c>-<c>Stage</c>/<c>Team</c>.</li>
///   <li>Check constraint for <c>Tournament</c> ensuring <c>TeamRegistrationDeadline</c> is before <c>StartDate</c>.</li>
/// </ul>
/// </item>
/// <item>
/// <b>DbSets:</b> Exposes sets for all domain entities, enabling CRUD operations.
/// </item>
/// </list>
/// </remarks>
public class ApplicationDBContext(DbContextOptions<ApplicationDBContext> options) : DbContext(options), IDomainDBContexts
{
    /// <summary>
    /// Configures model conventions, including custom date/time conversion.
    /// </summary>
    /// <param name="configurationBuilder">The builder for model configuration conventions.</param>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Conventions.Add(_ => new DateTimeToTimestampWithoutTimeZoneConvention());
        base.ConfigureConventions(configurationBuilder);
    }

    /// <summary>
    /// Configures entity mappings, relationships, property conversions, and database constraints.
    /// </summary>
    /// <param name="modelBuilder">The builder for entity models.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Store MatchType enum as string
        modelBuilder.Entity<Match>()
            .Property(match => match.Type)
            .HasConversion(
                value => value.ToString(),
                value => (MatchType)Enum.Parse(typeof(MatchType), value)
            );

        // Store StageType enum as string
        modelBuilder.Entity<Stage>()
            .Property(stage => stage.StageType)
            .HasConversion(
                value => value.ToString(),
                value => (StageType)Enum.Parse(typeof(StageType), value)
            );

        // Unique index for Stage Name and DivisionId
        modelBuilder.Entity<Stage>()
            .HasIndex(s => new { s.Name, s.DivisionId })
            .IsUnique()
            .HasDatabaseName("CONSTRAINT_UNIQUE_STAGE_NAME_AND_DIVISIONID");

        // Store StaffType enum as string
        modelBuilder.Entity<Staff>()
            .Property(staff => staff.Type)
            .HasConversion(
                value => value.ToString(),
                value => (StaffType)Enum.Parse(typeof(StaffType), value));

        // Team-Player relationship with cascade delete
        modelBuilder.Entity<Team>()
            .HasMany(team => team.Players)
            .WithOne(player => player.Team)
            .HasForeignKey(player => player.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        // Division-Stage relationship with cascade delete
        modelBuilder.Entity<Division>()
            .HasMany(division => division.Stages)
            .WithOne(stage => stage.Division)
            .HasForeignKey(stages => stages.DivisionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Store StaffType enum as string (duplicate for Staff entity)
        modelBuilder.Entity<Staff>()
            .Property(staffType => staffType.Type)
            .HasConversion(
                value => value.ToString(),
                value => (StaffType)Enum.Parse(typeof(StaffType), value)
            );

        // Tournament check constraint: TeamRegistrationDeadline < StartDate
        modelBuilder.Entity<Tournament>()
            .ToTable(t =>
                t.HasCheckConstraint(
                    "CK_Tournament_DeadlineBeforeStart",
                    "\"TeamRegistrationDeadline\" < \"StartDate\""
                    )
                );

        // StageTeamMatch-Stage relationship with cascade delete
        modelBuilder.Entity<StageTeamMatch>()
            .HasOne(stm => stm.Stage)
            .WithMany(stage => stage.StageTeamMatches)
            .HasForeignKey(stm => stm.StageId)
            .OnDelete(DeleteBehavior.Cascade);

        // StageTeamMatch-Team relationship with cascade delete
        modelBuilder.Entity<StageTeamMatch>()
            .HasOne(stm => stm.Team)
            .WithMany(team => team.StageTeamMatches)
            .HasForeignKey(stm => stm.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Gets or sets the teams in the database.
    /// </summary>
    public virtual required DbSet<Team> Teams { get; set; }

    /// <summary>
    /// Gets or sets the players in the database.
    /// </summary>
    public virtual required DbSet<Player> Players { get; set; }

    /// <summary>
    /// Gets or sets the tournaments in the database.
    /// </summary>
    public virtual required DbSet<Tournament> Tournaments { get; set; }

    /// <summary>
    /// Gets or sets the divisions in the database.
    /// </summary>
    public virtual required DbSet<Division> Divisions { get; set; }

    /// <summary>
    /// Gets or sets the matches in the database.
    /// </summary>
    public virtual required DbSet<Match> Matches { get; set; }

    /// <summary>
    /// Gets or sets the player statistics in the database.
    /// </summary>
    public virtual required DbSet<PlayerStatistic> PlayersStatistics { get; set; }

    /// <summary>
    /// Gets or sets the player sanctions in the database.
    /// </summary>
    public virtual required DbSet<PlayerSanction> PlayerSanctions { get; set; }

    /// <summary>
    /// Gets or sets the users in the database.
    /// </summary>
    public virtual required DbSet<User> Users { get; set; }

    /// <summary>
    /// Gets or sets the venues in the database.
    /// </summary>
    public virtual required DbSet<Venue> Venues { get; set; }

    /// <summary>
    /// Gets or sets the blog posts in the database.
    /// </summary>
    public virtual required DbSet<BlogPost> BlogPosts { get; set; }

    /// <summary>
    /// Gets or sets the staff members in the database.
    /// </summary>
    public virtual required DbSet<Staff> Staffs { get; set; }

    /// <summary>
    /// Gets or sets the stages in the database.
    /// </summary>
    public virtual required DbSet<Stage> Stages { get; set; }

    /// <summary>
    /// Gets or sets the stage-team matches in the database.
    /// </summary>
    public virtual required DbSet<StageTeamMatch> StageTeamMatches { get; set; }
}
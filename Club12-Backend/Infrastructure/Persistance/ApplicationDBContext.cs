using Domain.Entities.Models;

using Infrastructure.Persistance.Converters;

using Microsoft.EntityFrameworkCore;

using System;

namespace Infrastructure.Persistance;

/// <summary>
/// Application's main EF Core database context.
/// All entity mappings are defined in Infrastructure.Persistance.Configurations
/// and applied via ModelBuilder.ApplyConfigurationsFromAssembly.
/// </summary>
public class ApplicationDBContext(DbContextOptions<ApplicationDBContext> options)
    : DbContext(options), IClub12DBContext
{
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<DateTime>()
            .HaveColumnType("timestamp without time zone")
            .HaveConversion<UtcDateTimeConverter>();

        configurationBuilder
            .Properties<DateTime?>()
            .HaveColumnType("timestamp without time zone")
            .HaveConversion<NullableUtcDateTimeConverter>();

        base.ConfigureConventions(configurationBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDBContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public virtual required DbSet<Team> Teams { get; set; }
    public virtual required DbSet<Player> Players { get; set; }
    public virtual required DbSet<Tournament> Tournaments { get; set; }
    public virtual required DbSet<Division> Divisions { get; set; }
    public virtual required DbSet<DivisionPlayoffMapping> DivisionPlayoffMappings { get; set; }
    public virtual required DbSet<Match> Matches { get; set; }
    public virtual required DbSet<MatchSeries> MatchSeries { get; set; }
    public virtual required DbSet<PlayerStatistic> PlayersStatistics { get; set; }
    public virtual required DbSet<PlayerSanction> PlayerSanctions { get; set; }
    public virtual required DbSet<Venue> Venues { get; set; }
    public virtual required DbSet<BlogPost> BlogPosts { get; set; }
    public virtual required DbSet<Stage> Stages { get; set; }
    public virtual required DbSet<StageTeamMatch> StageTeamMatches { get; set; }
    public virtual required DbSet<Scorer> Scorers { get; set; }
    public virtual required DbSet<PlayerTeamRegistration> PlayerTeamRegistrations { get; set; }
    public virtual required DbSet<TeamTournamentRegistration> TeamTournamentRegistrations { get; set; }
    public virtual required DbSet<AuditLog> AuditLogs { get; set; }
}
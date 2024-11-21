

using Entities;
using Entities.Models.BlogPostEntity;
using Entities.Models.DivisionEntity;
using Entities.Models.MatchEntity;
using Entities.Models.PlayerEntity;
using Entities.Models.PlayerSanctionEntity;
using Entities.Models.PlayerStatisticEntity;
using Entities.Models.StaffEntity;
using Entities.Models.TeamEntity;
using Entities.Models.TournamentEntity;
using Entities.Models.UserEntity;
using Entities.Models.VenueEntity;

using Microsoft.EntityFrameworkCore;

using MatchType = Entities.Models.MatchTypeEnum.MatchType;
using StaffType = Entities.Models.StaffEnum.StaffType;
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
                value => (MatchType) Enum.Parse(typeof(MatchType), value)
            );

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

        modelBuilder.Entity<Staff>()
            .Property(staffType => staffType.Type)
            .HasConversion(
                value => value.ToString(),
                value => (StaffType) Enum.Parse(typeof(StaffType), value)
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
}
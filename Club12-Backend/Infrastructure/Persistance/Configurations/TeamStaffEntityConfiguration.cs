using Domain.Entities.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations;

/// <summary>
/// A team staff row carries no independent value once its team or tournament is gone, so both foreign keys cascade.
/// </summary>
public class TeamStaffEntityConfiguration : BaseEntityConfiguration<TeamStaff>
{
    protected override void ConfigureEntity(EntityTypeBuilder<TeamStaff> builder)
    {
        builder.ToTable(EntityConstants.Tables.TeamStaff, EntityConstants.Schema);

        builder.Property(s => s.FullName).IsRequired().HasMaxLength(150);
        builder.Property(s => s.Role).IsRequired();

        builder.HasIndex(s => new { s.TeamId, s.TournamentId });

        builder.HasOne(s => s.Team)
            .WithMany()
            .HasForeignKey(s => s.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Tournament)
            .WithMany()
            .HasForeignKey(s => s.TournamentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

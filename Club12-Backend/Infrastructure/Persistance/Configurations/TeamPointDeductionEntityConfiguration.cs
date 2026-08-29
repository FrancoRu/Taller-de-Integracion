using Domain.Entities.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations;

/// <summary>
/// EF configuration for <see cref="TeamPointDeduction"/> (deducción de puntos).
/// Deleting a division cascades its deductions away; the team FK is Restrict,
/// mirroring the PlayerSanction/Team relationship, so a team that still carries
/// deductions is not silently removed.
/// </summary>
public class TeamPointDeductionEntityConfiguration : BaseEntityConfiguration<TeamPointDeduction>
{
    protected override void ConfigureEntity(EntityTypeBuilder<TeamPointDeduction> builder)
    {
        builder.ToTable(EntityConstants.Tables.TeamPointDeduction, EntityConstants.Schema);

        builder.Property(d => d.Points).IsRequired();
        builder.Property(d => d.Reason).IsRequired().HasMaxLength(300);

        builder.HasIndex(d => d.DivisionId);

        builder.HasOne(d => d.Division)
            .WithMany()
            .HasForeignKey(d => d.DivisionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(d => d.Team)
            .WithMany()
            .HasForeignKey(d => d.TeamId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

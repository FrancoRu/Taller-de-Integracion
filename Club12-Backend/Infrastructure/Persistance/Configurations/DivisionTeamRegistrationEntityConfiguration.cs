using Domain.Entities.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations;

/// <summary>
/// A team is enrolled in the same division at most once, enforced by a unique index on TeamId and DivisionId.
/// </summary>
public class DivisionTeamRegistrationEntityConfiguration : BaseEntityConfiguration<DivisionTeamRegistration>
{
    protected override void ConfigureEntity(EntityTypeBuilder<DivisionTeamRegistration> builder)
    {
        builder.ToTable(EntityConstants.Tables.DivisionTeamRegistration, EntityConstants.Schema);

        builder.Property(r => r.TeamId).IsRequired();
        builder.Property(r => r.DivisionId).IsRequired();

        builder.HasIndex(r => new { r.TeamId, r.DivisionId }).IsUnique();

        builder.HasIndex(r => r.DivisionId);

        builder.HasOne(r => r.Team)
            .WithMany(t => t.DivisionTeamRegistrations)
            .HasForeignKey(r => r.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Division)
            .WithMany(d => d.DivisionTeamRegistrations)
            .HasForeignKey(r => r.DivisionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

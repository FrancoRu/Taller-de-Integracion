using Domain.Entities.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations;

/// <summary>
/// Maps the stable cross-season <see cref="Club"/> identity (HU-99). The
/// Club→Team relationship is deliberately optional and non-cascading: a Team's
/// <see cref="Team.ClubId"/> may be null (unlinked teams keep working), and
/// deleting a Club only nulls its teams' ClubId (SetNull) rather than deleting
/// the season Team rows — so the club layer is purely additive and can never
/// destroy per-season history.
/// </summary>
public class ClubEntityConfiguration : BaseEntityConfiguration<Club>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Club> builder)
    {
        builder.ToTable(EntityConstants.Tables.Club, EntityConstants.Schema);

        builder.Property(c => c.Name).IsRequired();
        builder.Property(c => c.Slug).IsRequired().HasMaxLength(220);
        builder.Property(c => c.LogoUrl);

        builder.HasIndex(c => c.Slug).IsUnique();

        builder.HasMany(c => c.Teams)
            .WithOne(t => t.Club)
            .HasForeignKey(t => t.ClubId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

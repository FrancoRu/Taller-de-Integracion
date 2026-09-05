using Domain.Entities.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations;

/// <summary>
/// Maps the Club to Team relationship as optional and non-cascading, so deleting a Club only nulls its teams' ClubId instead of removing them.
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

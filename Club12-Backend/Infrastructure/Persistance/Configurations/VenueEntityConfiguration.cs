using Domain.Entities.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations;

/// <summary>
/// PhotoUrl is intentionally not configured here: its [Url] attribute is a
/// validation-only concern with no corresponding database constraint.
/// </summary>
public class VenueEntityConfiguration : BaseEntityConfiguration<Venue>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Venue> builder)
    {
        builder.ToTable(EntityConstants.Tables.Venue, EntityConstants.Schema);

        builder.Property(v => v.Name).IsRequired().HasMaxLength(50);
        builder.Property(v => v.Address).IsRequired().HasMaxLength(200);
    }
}

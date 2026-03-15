using Domain.Entities.Models;
using Infrastructure.Persistance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations;

public class VenueEntityConfiguration : BaseEntityConfiguration<Venue>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Venue> builder)
    {
        builder.ToTable(EntityConstants.Tables.Venue, EntityConstants.Schema);

        builder.Property(v => v.Name).IsRequired().HasMaxLength(50);
        builder.Property(v => v.Address).IsRequired().HasMaxLength(200);
        // PhotoUrl: [Url] was a validation attribute only — no DB equivalent
    }
}

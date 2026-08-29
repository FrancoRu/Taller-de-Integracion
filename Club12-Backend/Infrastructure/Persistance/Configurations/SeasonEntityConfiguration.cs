using Application.Utils.Constants.Validation;

using Domain.Entities.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations;

public class SeasonEntityConfiguration : BaseEntityConfiguration<Season>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Season> builder)
    {
        builder.ToTable(EntityConstants.Tables.Season, EntityConstants.Schema);

        builder.Property(s => s.Name).IsRequired().HasMaxLength(SeasonFieldLengths.NameMaxLength);
        builder.Property(s => s.Slug).IsRequired().HasMaxLength(220);

        builder.HasIndex(s => s.Slug).IsUnique();

        // Season 1..* Tournament, with the tournament side optional: a
        // tournament may belong to at most one season, and deleting a season
        // detaches (SetNull) its tournaments instead of cascading — the season
        // is a purely additive grouping and must never delete a tournament.
        builder.HasMany(s => s.Tournaments)
            .WithOne(t => t.Season)
            .HasForeignKey(t => t.SeasonId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

using Domain.Entities.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations;

/// <summary>
/// MatchSeries has multiple foreign keys to Team (home, visitor, winner),
/// each configured explicitly below to avoid EF Core relationship ambiguity.
/// </summary>
public class MatchSeriesEntityConfiguration : BaseEntityConfiguration<MatchSeries>
{
    protected override void ConfigureEntity(EntityTypeBuilder<MatchSeries> builder)
    {
        builder.ToTable(EntityConstants.Tables.MatchSeries, EntityConstants.Schema);

        builder.Property(ms => ms.StageId).IsRequired();
        builder.Property(ms => ms.HomeTeamId).IsRequired();
        builder.Property(ms => ms.VisitorTeamId).IsRequired();

        builder.HasOne(ms => ms.Stage)
            .WithMany(s => s.MatchSeries)
            .HasForeignKey(ms => ms.StageId)
            .IsRequired();

        builder.HasOne(ms => ms.HomeTeam)
            .WithMany()
            .HasForeignKey(ms => ms.HomeTeamId)
            .IsRequired();

        builder.HasOne(ms => ms.VisitorTeam)
            .WithMany()
            .HasForeignKey(ms => ms.VisitorTeamId)
            .IsRequired();

        builder.HasOne(ms => ms.WinningTeam)
            .WithMany()
            .HasForeignKey(ms => ms.WinningTeamId)
            .IsRequired(false);

        builder.HasMany(ms => ms.Matches)
            .WithOne(m => m.Series)
            .HasForeignKey(m => m.SeriesId)
            .IsRequired(false);
    }
}

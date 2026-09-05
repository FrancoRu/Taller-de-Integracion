using Domain.Entities.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations;

/// <summary>
/// Match has multiple optional foreign keys to Team, each configured explicitly to avoid EF Core relationship ambiguity.
/// </summary>
public class MatchEntityConfiguration : BaseEntityConfiguration<Match>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Match> builder)
    {
        builder.ToTable(EntityConstants.Tables.Match, EntityConstants.Schema);

        builder.Property(m => m.Type).IsRequired().HasConversion<string>();
        builder.Property(m => m.Slug).IsRequired().HasMaxLength(220);
        builder.Property(m => m.IsFinished).IsRequired();
        builder.Property(m => m.Status).IsRequired().HasConversion<string>();
        builder.Property(m => m.StageId).IsRequired();

        builder.HasIndex(m => m.Slug).IsUnique();

        builder.HasOne(m => m.Stage)
            .WithMany(s => s.Matches)
            .HasForeignKey(m => m.StageId)
            .IsRequired();

        builder.HasOne(m => m.HomeTeam)
            .WithMany()
            .HasForeignKey(m => m.HomeTeamId)
            .IsRequired(false);

        builder.HasOne(m => m.VisitorTeam)
            .WithMany()
            .HasForeignKey(m => m.VisitorTeamId)
            .IsRequired(false);

        builder.HasOne(m => m.WinningTeam)
            .WithMany()
            .HasForeignKey(m => m.WinningTeamId)
            .IsRequired(false);

        builder.HasOne(m => m.Venue)
            .WithMany()
            .HasForeignKey(m => m.VenueId)
            .IsRequired(false);

        builder.HasOne(m => m.Series)
            .WithMany(ms => ms.Matches)
            .HasForeignKey(m => m.SeriesId)
            .IsRequired(false);
    }
}

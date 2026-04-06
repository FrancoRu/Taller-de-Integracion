using Domain.Entities.Models;
using Infrastructure.Persistance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations;

public class MatchEntityConfiguration : BaseEntityConfiguration<Match>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Match> builder)
    {
        builder.ToTable(EntityConstants.Tables.Match, EntityConstants.Schema);

        builder.Property(m => m.Type).IsRequired().HasConversion<string>();
        builder.Property(m => m.IsFinished).IsRequired();
        builder.Property(m => m.StageId).IsRequired();

        // Computed service-layer collections — not persisted
        //builder.Ignore(m => m.HomeScorers);
        //builder.Ignore(m => m.VisitorScorers);

        builder.HasOne(m => m.Stage)
            .WithMany(s => s.Matches)
            .HasForeignKey(m => m.StageId)
            .IsRequired();

        // Multiple optional FKs to Team — must be explicit to avoid ambiguity
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
    }
}

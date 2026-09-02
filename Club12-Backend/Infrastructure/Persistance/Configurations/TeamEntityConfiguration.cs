using Domain.Entities.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations;

/// <summary>
/// TournamentId is an optional FK since a Team may exist without a tournament.
/// Deleting a Team cascades to its Players.
/// </summary>
public class TeamEntityConfiguration : BaseEntityConfiguration<Team>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Team> builder)
    {
        builder.ToTable(EntityConstants.Tables.Team, EntityConstants.Schema);

        builder.Property(t => t.Name).IsRequired();
        builder.Property(t => t.Slug).IsRequired().HasMaxLength(220);
        builder.Property(t => t.ThreeLetterCode).IsRequired();
        builder.Property(t => t.LogoUrl).IsRequired();
        builder.Property(t => t.ShirtColor).IsRequired();
        builder.Property(t => t.JerseyStyle).IsRequired().HasMaxLength(20).HasDefaultValue("solid");
        builder.Property(t => t.ShirtSecondaryColor).HasMaxLength(9);
        builder.Property(t => t.ShirtTertiaryColor).HasMaxLength(9);

        builder.HasIndex(t => t.Slug).IsUnique();

        builder.HasOne(t => t.Tournament)
            .WithMany(tourn => tourn.Teams)
            .HasForeignKey(t => t.TournamentId)
            .IsRequired(false);

        builder.HasMany(t => t.Players)
            .WithOne(p => p.Team)
            .HasForeignKey(p => p.TeamId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

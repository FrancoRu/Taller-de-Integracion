using Domain.Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations;

public class TeamEntityConfiguration : BaseEntityConfiguration<Team>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Team> builder)
    {
        builder.ToTable(EntityConstants.Tables.Team, EntityConstants.Schema);

        builder.Property(t => t.Name).IsRequired();
        builder.Property(t => t.ThreeLetterCode).IsRequired();
        builder.Property(t => t.LogoUrl).IsRequired();
        builder.Property(t => t.ShirtColor).IsRequired();

        // TournamentId: optional FK (Team may belong to a tournament or not)
        builder.HasOne(t => t.Tournament)
            .WithMany(tourn => tourn.Teams)
            .HasForeignKey(t => t.TournamentId)
            .IsRequired(false);

        // Cascade: deleting a Team removes its Players
        builder.HasMany(t => t.Players)
            .WithOne(p => p.Team)
            .HasForeignKey(p => p.TeamId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

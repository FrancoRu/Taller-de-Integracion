using Domain.Entities.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations;

/// <summary>
/// A team may be registered to the same tournament only once, enforced by a unique index on TeamId and TournamentId, but may hold independent registrations across multiple tournaments at once.
/// </summary>
public class TeamTournamentRegistrationEntityConfiguration : BaseEntityConfiguration<TeamTournamentRegistration>
{
    protected override void ConfigureEntity(EntityTypeBuilder<TeamTournamentRegistration> builder)
    {
        builder.ToTable(EntityConstants.Tables.TeamTournamentRegistration, EntityConstants.Schema);

        builder.Property(r => r.TeamId).IsRequired();
        builder.Property(r => r.TournamentId).IsRequired();

        builder.HasIndex(r => new { r.TeamId, r.TournamentId }).IsUnique();

        builder.HasOne(r => r.Team)
            .WithMany(t => t.TeamTournamentRegistrations)
            .HasForeignKey(r => r.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Tournament)
            .WithMany()
            .HasForeignKey(r => r.TournamentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

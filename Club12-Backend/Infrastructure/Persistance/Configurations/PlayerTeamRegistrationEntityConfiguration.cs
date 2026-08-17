using Domain.Entities.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations;

/// <summary>
/// A player may have at most one registration per tournament (season) —
/// enforced by the unique index on PlayerId+TournamentId — so a player can
/// never be on two teams in the same season.
/// </summary>
public class PlayerTeamRegistrationEntityConfiguration : BaseEntityConfiguration<PlayerTeamRegistration>
{
    protected override void ConfigureEntity(EntityTypeBuilder<PlayerTeamRegistration> builder)
    {
        builder.ToTable(EntityConstants.Tables.PlayerTeamRegistration, EntityConstants.Schema);

        builder.Property(r => r.PlayerId).IsRequired();
        builder.Property(r => r.TeamId).IsRequired();
        builder.Property(r => r.TournamentId).IsRequired();

        builder.HasIndex(r => new { r.PlayerId, r.TournamentId }).IsUnique();

        builder.HasOne(r => r.Player)
            .WithMany(p => p.PlayerTeamRegistrations)
            .HasForeignKey(r => r.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Team)
            .WithMany(t => t.PlayerTeamRegistrations)
            .HasForeignKey(r => r.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Tournament)
            .WithMany()
            .HasForeignKey(r => r.TournamentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

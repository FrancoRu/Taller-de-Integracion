using Domain.Entities.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations;

/// <summary>
/// A player may have at most one registration per tournament season, enforced by a unique index on PlayerId and TournamentId, so a player can never be on two teams in one season.
/// </summary>
public class PlayerTeamRegistrationEntityConfiguration : BaseEntityConfiguration<PlayerTeamRegistration>
{
    protected override void ConfigureEntity(EntityTypeBuilder<PlayerTeamRegistration> builder)
    {
        builder.ToTable(EntityConstants.Tables.PlayerTeamRegistration, EntityConstants.Schema);

        builder.Property(r => r.PlayerId).IsRequired();
        builder.Property(r => r.TeamId).IsRequired();
        builder.Property(r => r.TournamentId).IsRequired();

        // Status is persisted as the enum name string, mirroring other enum columns in this DB, including Match.Status, PlayerStatistic.Type, and PlayerSanction.AppealStatus.
        builder.Property(r => r.MedicalRecordStatus).IsRequired().HasConversion<string>();
        builder.Property(r => r.MedicalRecordFileUrl);
        builder.Property(r => r.MedicalRecordFileName);
        builder.Property(r => r.MedicalRecordReviewReason);
        builder.Property(r => r.MedicalRecordReviewedAt);

        builder.HasIndex(r => new { r.PlayerId, r.TournamentId }).IsUnique();

        // Both Npgsql and SQLite treat NULLs as distinct in a unique index, so this allows any number of players with no assigned dorsal while rejecting duplicate non-null dorsals, with no provider-specific filtered index required.
        builder.Property(r => r.JerseyNumber);
        builder.HasIndex(r => new { r.TeamId, r.TournamentId, r.JerseyNumber }).IsUnique();

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

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

        // Medical-record / eligibility (HU-55/57/58). Status is persisted as
        // the enum name (string), mirroring the other enum columns in this DB
        // (Match.Status, PlayerStatistic.Type, PlayerSanction.AppealStatus).
        builder.Property(r => r.MedicalRecordStatus).IsRequired().HasConversion<string>();
        builder.Property(r => r.MedicalRecordFileUrl);
        builder.Property(r => r.MedicalRecordFileName);
        builder.Property(r => r.MedicalRecordReviewReason);
        builder.Property(r => r.MedicalRecordReviewedAt);

        builder.HasIndex(r => new { r.PlayerId, r.TournamentId }).IsUnique();

        // Jersey number / dorsal (HU-54). Unique within the same team +
        // tournament. Both Npgsql and SQLite treat NULLs as distinct in a
        // unique index, so this allows any number of players with no assigned
        // dorsal (NULL) while rejecting duplicate non-null dorsals — no
        // provider-specific filtered index required.
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

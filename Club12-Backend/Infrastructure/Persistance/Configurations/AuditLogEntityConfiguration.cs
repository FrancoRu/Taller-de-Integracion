using Domain.Entities.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations;

/// <summary>
/// EF Core mapping for the sensitive-action audit trail.
/// </summary>
public class AuditLogEntityConfiguration : BaseEntityConfiguration<AuditLog>
{
    protected override void ConfigureEntity(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable(EntityConstants.Tables.AuditLog, EntityConstants.Schema);

        // Action persisted as the enum name (string), mirroring the other enum
        // columns in this DB (Match.Status, PlayerTeamRegistration.MedicalRecordStatus).
        builder.Property(a => a.Action).IsRequired().HasConversion<string>();
        builder.Property(a => a.Actor).IsRequired().HasMaxLength(256);
        builder.Property(a => a.TargetType).HasMaxLength(128);
        builder.Property(a => a.TargetId).HasMaxLength(128);
        builder.Property(a => a.TargetName).HasMaxLength(256);
        builder.Property(a => a.Detail).HasMaxLength(1024);

        builder.HasIndex(a => a.Action);
    }
}

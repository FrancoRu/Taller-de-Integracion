using Domain.Entities.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations;

public class BackupRecordEntityConfiguration : BaseEntityConfiguration<BackupRecord>
{
    protected override void ConfigureEntity(EntityTypeBuilder<BackupRecord> builder)
    {
        builder.ToTable(EntityConstants.Tables.BackupRecord, EntityConstants.Schema);

        builder.Property(b => b.StoragePath).IsRequired().HasMaxLength(500);
        builder.Property(b => b.SizeBytes).IsRequired();
        builder.Property(b => b.Origin).IsRequired().HasConversion<string>();
    }
}

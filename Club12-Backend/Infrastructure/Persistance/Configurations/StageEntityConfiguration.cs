using Domain.Entities.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations;

public class StageEntityConfiguration : BaseEntityConfiguration<Stage>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Stage> builder)
    {
        builder.ToTable(EntityConstants.Tables.Stage, EntityConstants.Schema);

        builder.Property(s => s.Name).IsRequired().HasMaxLength(100);
        builder.Property(s => s.Description).HasMaxLength(1000);
        builder.Property(s => s.StageType).IsRequired().HasConversion<string>();
        builder.Property(s => s.IsActive).IsRequired();
        builder.Property(s => s.StartDate).IsRequired();
        builder.Property(s => s.EndDate).IsRequired();
        builder.Property(s => s.DivisionId).IsRequired();
        builder.Property(s => s.BracketName).HasMaxLength(100);
        builder.Property(s => s.BestOf).IsRequired().HasDefaultValue(1);
        builder.Property(s => s.RoundRobinLegs).IsRequired().HasDefaultValue(1);

        builder.HasIndex(s => new { s.Name, s.DivisionId })
            .IsUnique()
            .HasDatabaseName(EntityConstants.Indexes.UniqueStageNameDivision);
    }
}

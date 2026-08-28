using Domain.Entities.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations;

public class DivisionPlayoffMappingEntityConfiguration : BaseEntityConfiguration<DivisionPlayoffMapping>
{
    protected override void ConfigureEntity(EntityTypeBuilder<DivisionPlayoffMapping> builder)
    {
        builder.ToTable(EntityConstants.Tables.DivisionPlayoffMapping, EntityConstants.Schema);

        builder.Property(m => m.FromPosition).IsRequired();
        builder.Property(m => m.ToPosition).IsRequired();
        builder.Property(m => m.Destination).IsRequired().HasMaxLength(100);

        builder.HasIndex(m => m.DivisionId);
    }
}

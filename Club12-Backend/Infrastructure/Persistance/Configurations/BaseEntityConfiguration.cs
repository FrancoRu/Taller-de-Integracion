using Domain.Entities.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations;

/// <summary>
/// Base EF Core configuration shared by every entity, mapping the primary key, audit timestamps, and a DateCreated index.
/// </summary>
public abstract class BaseEntityConfiguration<TEntity> : IEntityTypeConfiguration<TEntity>
    where TEntity : EntityBase
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<TEntity> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .ValueGeneratedOnAdd();

        builder.Property(e => e.DateCreated)
            .IsRequired();

        builder.Property(e => e.DateUpdated);

        builder.HasIndex(e => e.DateCreated)
            .HasDatabaseName($"IX_{typeof(TEntity).Name}_CreatedAt");

        ConfigureEntity(builder);
    }

    /// <summary>
    /// Override to configure entity-specific table, columns, indexes, and relationships.
    /// </summary>
    protected abstract void ConfigureEntity(EntityTypeBuilder<TEntity> builder);
}

using Domain.Entities.Models;
using Domain.Enums;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations;

public class DivisionEntityConfiguration : BaseEntityConfiguration<Division>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Division> builder)
    {
        builder.ToTable(EntityConstants.Tables.Division, EntityConstants.Schema);

        builder.Property(d => d.Name).IsRequired().HasMaxLength(30);
        builder.Property(d => d.Slug).IsRequired().HasMaxLength(220);

        builder.HasIndex(d => d.Slug).IsUnique();

        builder.HasOne(d => d.Tournament)
            .WithMany(t => t.Divisions)
            .HasForeignKey(d => d.TournamentId)
            .IsRequired();

        builder.Property(d => d.PointsForWin).IsRequired().HasDefaultValue(2);
        builder.Property(d => d.PointsForLoss).IsRequired().HasDefaultValue(1);
        builder.Property(d => d.QualifiersPerGroup).IsRequired().HasDefaultValue(1);
        builder.Property(d => d.Category).IsRequired().HasDefaultValue(TournamentCategory.Masculine);

        builder.HasMany(d => d.Stages)
            .WithOne(s => s.Division)
            .HasForeignKey(s => s.DivisionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(d => d.PlayoffMappings)
            .WithOne(m => m.Division)
            .HasForeignKey(m => m.DivisionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

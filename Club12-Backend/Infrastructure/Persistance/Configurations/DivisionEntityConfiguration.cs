using Domain.Entities.Models;
using Infrastructure.Persistance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations;

public class DivisionEntityConfiguration : BaseEntityConfiguration<Division>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Division> builder)
    {
        builder.ToTable(EntityConstants.Tables.Division, EntityConstants.Schema);

        builder.Property(d => d.Name).IsRequired().HasMaxLength(30);

        builder.HasOne(d => d.Tournament)
            .WithMany(t => t.Divisions)
            .HasForeignKey(d => d.TournamentId)
            .IsRequired();

        builder.HasMany(d => d.Stages)
            .WithOne(s => s.Division)
            .HasForeignKey(s => s.DivisionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

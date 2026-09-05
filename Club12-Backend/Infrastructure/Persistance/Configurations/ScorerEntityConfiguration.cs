using Domain.Entities.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations;

/// <summary>
/// Scorer is a service-layer read-model with no DataAnnotations, so there is nothing to migrate beyond the mappings configured here.
/// </summary>
public class ScorerEntityConfiguration : BaseEntityConfiguration<Scorer>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Scorer> builder)
    {
        builder.ToTable(EntityConstants.Tables.Scorer, EntityConstants.Schema);

        builder.Property(s => s.PlayerId).IsRequired();
        builder.Property(s => s.Points).IsRequired();
        builder.Property(s => s.MatchId).IsRequired();

        builder.HasOne(s => s.Player)
               .WithMany(p => p.Scorers)
               .HasForeignKey(s => s.PlayerId);

        builder.HasOne(s => s.Match)
               .WithMany(m => m.Scorers)
               .HasForeignKey(s => s.MatchId);
    }
}
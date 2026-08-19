using Domain.Entities.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations;

public class PlayerStatisticEntityConfiguration : BaseEntityConfiguration<PlayerStatistic>
{
    protected override void ConfigureEntity(EntityTypeBuilder<PlayerStatistic> builder)
    {
        builder.ToTable(EntityConstants.Tables.PlayerStatistic, EntityConstants.Schema);

        builder.Property(ps => ps.Value).IsRequired();

        builder.Property(ps => ps.Type).IsRequired().HasConversion<string>();

        builder.HasOne(ps => ps.Match)
            .WithMany(m => m.PlayerStatistics)
            .HasForeignKey(ps => ps.MatchId)
            .IsRequired();

        builder.HasOne(ps => ps.Player)
            .WithMany()
            .HasForeignKey(ps => ps.PlayerId)
            .IsRequired();
    }
}

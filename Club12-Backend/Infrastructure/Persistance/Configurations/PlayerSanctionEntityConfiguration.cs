using Domain.Entities.Models;
using Infrastructure.Persistance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations;

public class PlayerSanctionEntityConfiguration : BaseEntityConfiguration<PlayerSanction>
{
    protected override void ConfigureEntity(EntityTypeBuilder<PlayerSanction> builder)
    {
        builder.ToTable(EntityConstants.Tables.PlayerSanction, EntityConstants.Schema);

        builder.Property(ps => ps.Duration).IsRequired();
        builder.Property(ps => ps.IssuedDate).IsRequired();
        builder.Property(ps => ps.Description).IsRequired().HasMaxLength(255);
        builder.Property(ps => ps.AppealStatus).IsRequired().HasConversion<string>();
        builder.Property(ps => ps.AppealReason).HasMaxLength(1000);
        builder.Property(ps => ps.AppealResolution).HasMaxLength(1000);

        builder.HasOne(ps => ps.Player)
            .WithMany()
            .HasForeignKey(ps => ps.PlayerId)
            .IsRequired();

        builder.HasOne(ps => ps.Match)
            .WithMany()
            .HasForeignKey(ps => ps.MatchId)
            .IsRequired();
    }
}

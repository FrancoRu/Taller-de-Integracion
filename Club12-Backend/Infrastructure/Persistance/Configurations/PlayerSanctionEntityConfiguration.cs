using Domain.Entities.Models;

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
        builder.Property(ps => ps.Slug).IsRequired().HasMaxLength(220);
        builder.Property(ps => ps.SubjectType).IsRequired().HasConversion<string>();
        builder.Property(ps => ps.StaffName).HasMaxLength(255);
        builder.Property(ps => ps.AppealStatus).IsRequired().HasConversion<string>();
        builder.Property(ps => ps.AppealReason).HasMaxLength(1000);
        builder.Property(ps => ps.AppealResolution).HasMaxLength(1000);

        builder.HasIndex(ps => ps.Slug).IsUnique();

        // OnDelete is stated explicitly because the existing migrations already applied Cascade for Player and Restrict for Team, and without it the optional FKs would default to NoAction and drift from the persisted schema.
        builder.HasOne(ps => ps.Player)
            .WithMany()
            .HasForeignKey(ps => ps.PlayerId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ps => ps.Team)
            .WithMany()
            .HasForeignKey(ps => ps.TeamId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ps => ps.Match)
            .WithMany()
            .HasForeignKey(ps => ps.MatchId)
            .IsRequired();
    }
}

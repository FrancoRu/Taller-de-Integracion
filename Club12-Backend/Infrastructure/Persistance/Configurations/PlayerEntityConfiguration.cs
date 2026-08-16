using Domain.Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations;

/// <summary>
/// FullName is ignored because it is a computed C# property, not a DB column.
/// </summary>
public class PlayerEntityConfiguration : BaseEntityConfiguration<Player>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Player> builder)
    {
        builder.ToTable(EntityConstants.Tables.Player, EntityConstants.Schema);

        builder.Property(p => p.FirstName).IsRequired().HasMaxLength(70);
        builder.Property(p => p.SecondName).HasMaxLength(70);
        builder.Property(p => p.LastName).IsRequired().HasMaxLength(70);
        builder.Property(p => p.DocumentNumber).IsRequired().HasMaxLength(15);
        builder.Property(p => p.IsSanctioned).IsRequired();
        builder.Property(p => p.BirthDate).IsRequired();
        builder.Property(p => p.SocialSecurity).IsRequired().HasMaxLength(100);

        builder.Ignore(p => p.FullName);

        builder.HasIndex(p => p.DocumentNumber).IsUnique();
    }
}

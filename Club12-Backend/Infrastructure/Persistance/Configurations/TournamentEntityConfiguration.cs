using Domain.Entities.Models;
using Domain.Enums;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations;

public class TournamentEntityConfiguration : BaseEntityConfiguration<Tournament>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Tournament> builder)
    {
        builder.ToTable(EntityConstants.Tables.Tournament, EntityConstants.Schema, t =>
            t.HasCheckConstraint(
                EntityConstants.CheckConstraints.TournamentDeadlineBeforeStart,
                "\"TeamRegistrationDeadline\" < \"StartDate\""
            )
        );

        builder.Property(t => t.Name).IsRequired();
        builder.Property(t => t.Slug).IsRequired().HasMaxLength(220);
        builder.Property(t => t.Description).IsRequired();
        builder.Property(t => t.TeamRegistrationDeadline).IsRequired();
        builder.Property(t => t.StartDate).IsRequired();
        builder.Property(t => t.MaxTeams).IsRequired();
        builder.Property(t => t.MinTeams).IsRequired();
        builder.Property(t => t.Status).HasDefaultValue(TournamentStatus.Scheduled).IsRequired();

        builder.HasIndex(t => t.Slug).IsUnique();
    }
}

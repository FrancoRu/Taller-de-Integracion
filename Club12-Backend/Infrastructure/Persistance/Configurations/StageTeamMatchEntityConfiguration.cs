using Domain.Entities.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations;

public class StageTeamMatchEntityConfiguration : BaseEntityConfiguration<StageTeamMatch>
{
    protected override void ConfigureEntity(EntityTypeBuilder<StageTeamMatch> builder)
    {
        builder.ToTable(EntityConstants.Tables.StageTeamMatch, EntityConstants.Schema);

        builder.Property(stm => stm.StageId).IsRequired();
        builder.Property(stm => stm.TeamId).IsRequired();

        builder.HasOne(stm => stm.Stage)
            .WithMany(s => s.StageTeamMatches)
            .HasForeignKey(stm => stm.StageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(stm => stm.Team)
            .WithMany(t => t.StageTeamMatches)
            .HasForeignKey(stm => stm.TeamId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

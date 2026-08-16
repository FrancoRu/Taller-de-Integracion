using Domain.Entities.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistance.Configurations;

public class BlogPostEntityConfiguration : BaseEntityConfiguration<BlogPost>
{
    protected override void ConfigureEntity(EntityTypeBuilder<BlogPost> builder)
    {
        builder.ToTable(EntityConstants.Tables.BlogPost, EntityConstants.Schema);

        builder.Property(b => b.Author).IsRequired().HasMaxLength(50);
        builder.Property(b => b.Title).IsRequired();
        builder.Property(b => b.PhotoUrl).HasMaxLength(2048);
        builder.Property(b => b.MarkdownText).IsRequired();
    }
}

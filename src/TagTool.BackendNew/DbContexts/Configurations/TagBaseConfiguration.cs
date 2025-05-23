using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TagTool.BackendNew.Contracts;
using TagTool.BackendNew.Contracts.Entities;
using TagTool.BackendNew.Entities;

namespace TagTool.BackendNew.DbContexts.Configurations;

public class TagBaseConfiguration : IEntityTypeConfiguration<TagBase>
{
    public void Configure(EntityTypeBuilder<TagBase> builder)
    {
        builder
            .HasMany(e => e.TaggedItems)
            .WithMany(e => e.Tags)
            .UsingEntity<TagBaseTaggableItem>();

        builder
            .HasIndex(tag => tag.Text)
            .IsUnique();

        builder
            .Property(tag => tag.Text)
            .HasMaxLength(60)
            .UseCollation("NOCASE");
    }
}

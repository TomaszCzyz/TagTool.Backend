using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TagTool.Backend.Models;
using File = TagTool.Backend.Models.File;

namespace TagTool.Backend.DbContext;

public class TagEntityConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder
            .HasMany<File>()
            .WithMany(file => file.Tags)
            .UsingEntity<FileTag>(e => e.HasKey(ft => new {ft.TagId, ft.FileId}));

        builder
            .HasMany<Group>()
            .WithMany(group => group.Tags);
    }
}

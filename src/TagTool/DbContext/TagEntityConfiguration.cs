using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TagTool.Models;
using File = TagTool.Models.File;

namespace TagTool.DbContext;

public class TagEntityConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.HasMany<Group>().WithMany(group => group.Tags);
        builder.HasMany<File>().WithMany(file => file.Tags);
    }
}

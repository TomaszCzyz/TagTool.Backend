using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TagTool.BackendNew.Contracts;
using TagTool.BackendNew.Contracts.Entities;

namespace TagTool.BackendNew.TaggableItems.TaggableFile;

/// Adds <see cref="TaggableFile"/> to tracked models.
public class TaggableFileConfiguration : IEntityTypeConfiguration<TaggableFile>
{
    public void Configure(EntityTypeBuilder<TaggableFile> builder)
    {
        builder
            .HasBaseType<TaggableItem>();

        builder
            .Property(o => o.Path)
            .HasMaxLength(1000);
    }
}

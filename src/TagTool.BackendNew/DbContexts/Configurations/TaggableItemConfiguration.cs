using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TagTool.BackendNew.Entities;

namespace TagTool.BackendNew.DbContexts.Configurations;

public class TaggableItemConfiguration : IEntityTypeConfiguration<TaggableItem>
{
    public void Configure(EntityTypeBuilder<TaggableItem> builder)
    {
        builder
            .UseTpcMappingStrategy()
            .HasKey(e => e.Id);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TagTool.BackendNew.Contracts;

namespace TagTool.BackendNew.DbContexts.Configurations;

public class TaggableItemConfiguration : IEntityTypeConfiguration<TaggableItem>
{
    public void Configure(EntityTypeBuilder<TaggableItem> builder)
    {
        builder
            .UseTpcMappingStrategy()
            .HasKey(e => e.Id);

        builder
            .Property(e => e.Id)
            .ValueGeneratedNever();
    }
}

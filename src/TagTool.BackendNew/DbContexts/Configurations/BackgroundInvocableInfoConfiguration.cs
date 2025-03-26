using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TagTool.BackendNew.Entities;

namespace TagTool.BackendNew.DbContexts.Configurations;

public class BackgroundInvocableInfoConfiguration : IEntityTypeConfiguration<BackgroundInvocableInfo>
{
    public void Configure(EntityTypeBuilder<BackgroundInvocableInfo> builder)
    {
        builder
            .Property(tag => tag.InvocableType)
            .IsRequired()
            .HasConversion(
                type => type.AssemblyQualifiedName,
                s => Type.GetType(s!)!);

        builder
            .Property(tag => tag.InvocablePayloadType)
            .IsRequired()
            .HasConversion(
                type => type.AssemblyQualifiedName,
                s => Type.GetType(s!)!);
    }
}

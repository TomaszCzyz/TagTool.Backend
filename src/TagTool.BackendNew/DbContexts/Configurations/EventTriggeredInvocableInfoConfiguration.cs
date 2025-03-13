using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TagTool.BackendNew.Entities;

namespace TagTool.BackendNew.DbContexts.Configurations;

public class EventTriggeredInvocableInfoConfiguration : IEntityTypeConfiguration<EventTriggeredInvocableInfo>
{
    public void Configure(EntityTypeBuilder<EventTriggeredInvocableInfo> builder)
    {
        builder
            .Property(tag => tag.InvocableType)
            .HasConversion(
                type => type.AssemblyQualifiedName,
                s => Type.GetType(s!)!);

        builder
            .Property(tag => tag.InvocablePayloadType)
            .HasConversion(
                type => type.AssemblyQualifiedName,
                s => Type.GetType(s!)!);
    }
}

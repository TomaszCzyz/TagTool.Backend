using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TagTool.BackendNew.Entities;

namespace TagTool.BackendNew.DbContexts.Configurations;

public class CronTriggeredInvocableInfoConfiguration : IEntityTypeConfiguration<CronTriggeredInvocableInfo>
{
    public void Configure(EntityTypeBuilder<CronTriggeredInvocableInfo> builder)
    {
        builder
            .Property(tag => tag.CronExpression)
            .HasMaxLength(11);

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

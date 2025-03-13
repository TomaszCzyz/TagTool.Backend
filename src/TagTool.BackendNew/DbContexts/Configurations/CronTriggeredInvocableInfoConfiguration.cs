using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TagTool.BackendNew.Entities;
using TagTool.BackendNew.Invocables.Common;

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
                type => type.FullName,
                s => Type.GetType(s!)!);

        builder
            .Property(p => p.Payload)
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => JsonSerializer.Deserialize<PayloadWithQuery>(v, JsonSerializerOptions.Default)!);
    }
}

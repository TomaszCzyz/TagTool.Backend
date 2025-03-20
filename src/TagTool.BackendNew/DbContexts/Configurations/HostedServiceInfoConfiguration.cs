using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TagTool.BackendNew.Entities;

namespace TagTool.BackendNew.DbContexts.Configurations;

public class HostedServiceInfoConfiguration : IEntityTypeConfiguration<HostedServiceInfo>
{
    public void Configure(EntityTypeBuilder<HostedServiceInfo> builder)
    {
        builder
            .Property(tag => tag.HostedServiceType)
            .IsRequired()
            .HasConversion(
                type => type.AssemblyQualifiedName,
                s => Type.GetType(s!)!);

        builder
            .Property(tag => tag.HostedServicePayloadType)
            .IsRequired()
            .HasConversion(
                type => type.AssemblyQualifiedName,
                s => Type.GetType(s!)!);
    }
}

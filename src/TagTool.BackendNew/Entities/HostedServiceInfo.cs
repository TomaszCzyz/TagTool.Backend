namespace TagTool.BackendNew.Entities;

public class HostedServiceInfo
{
    public Guid Id { get; set; }

    public required Type HostedServiceType { get; init; }

    public required Type HostedServicePayloadType { get; init; }

    public required string Payload { get; set; }
}

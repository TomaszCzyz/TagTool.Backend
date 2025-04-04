namespace TagTool.BackendNew.Entities;

public class BackgroundInvocableInfo
{
    public Guid Id { get; set; }

    public required Type InvocableType { get; init; }

    public required Type InvocablePayloadType { get; init; }

    public required string Payload { get; set; }
}

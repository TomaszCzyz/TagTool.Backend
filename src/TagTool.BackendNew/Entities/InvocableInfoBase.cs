namespace TagTool.BackendNew.Entities;

public abstract class InvocableInfoBase : ITimestamped
{
    public Guid Id { get; set; }

    public required string InvocableId { get; set; }

    public required Type InvocableType { get; init; }

    public required Type InvocablePayloadType { get; init; }

    public required string Payload { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }
}

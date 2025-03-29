using TagTool.BackendNew.Contracts;

namespace TagTool.BackendNew.Entities;

public class CronTriggeredInvocableInfo
{
    public Guid Id { get; set; }

    public required Type InvocableType { get; init; }

    public required Type InvocablePayloadType { get; init; }

    public required string CronExpression { get; set; }

    public required List<TagQueryPart> TagQuery { get; set; }

    public required string Payload { get; set; }
}

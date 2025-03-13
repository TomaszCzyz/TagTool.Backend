using TagTool.BackendNew.Models;

namespace TagTool.BackendNew.Entities;

public class CronTriggeredInvocableInfo
{
    public Guid Id { get; set; }

    public required Type InvocableType { get; init; }

    public required string CronExpression { get; set; }

    public required List<TagQueryPart> TagQuery { get; set; }

    public required object Payload { get; set; }
}
